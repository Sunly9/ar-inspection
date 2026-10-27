package main

import (
	"database/sql"
	"errors"
	"os"
	"path/filepath"

	_ "modernc.org/sqlite" // 纯 Go 的 SQLite 驱动，不需要 CGO / gcc
)

var errIssueNotFound = errors.New("issue not found")

const schema = `
CREATE TABLE IF NOT EXISTS issues (
	id          TEXT PRIMARY KEY,
	title       TEXT NOT NULL,
	description TEXT NOT NULL DEFAULT '',
	priority    TEXT NOT NULL,
	status      TEXT NOT NULL DEFAULT 'open',
	x           REAL NOT NULL,
	y           REAL NOT NULL,
	z           REAL NOT NULL,
	normal_x    REAL NOT NULL DEFAULT 0,
	normal_y    REAL NOT NULL DEFAULT 1,
	normal_z    REAL NOT NULL DEFAULT 0,
	reporter    TEXT NOT NULL DEFAULT '',
	status_changed_by TEXT NOT NULL DEFAULT '',
	created_at  TEXT NOT NULL,
	updated_at  TEXT NOT NULL
);`

type Store struct {
	db *sql.DB
}

func openStore(path string) (*Store, error) {
	if dir := filepath.Dir(path); dir != "" && dir != "." {
		if err := os.MkdirAll(dir, 0o755); err != nil {
			return nil, err
		}
	}
	db, err := sql.Open("sqlite", path)
	if err != nil {
		return nil, err
	}
	// SQLite 单写者，限制为 1 个连接，避免高并发下 database is locked
	db.SetMaxOpenConns(1)
	if _, err := db.Exec(schema); err != nil {
		db.Close()
		return nil, err
	}
	// 旧库补列：normal_* 是后加的字段，老数据默认朝上
	if err := migrate(db); err != nil {
		db.Close()
		return nil, err
	}
	return &Store{db: db}, nil
}

// migrate 给已存在的旧表补上新增的列（SQLite 没有 IF NOT EXISTS 语法，先查再加）。
func migrate(database *sql.DB) error {
	rows, err := database.Query(`PRAGMA table_info(issues)`)
	if err != nil {
		return err
	}

	columns := map[string]bool{}
	for rows.Next() {
		var (
			cid, notNull, pk int
			name, dataType   string
			defaultValue     sql.NullString
		)
		if err := rows.Scan(&cid, &name, &dataType, &notNull, &defaultValue, &pk); err != nil {
			rows.Close()
			return err
		}
		columns[name] = true
	}
	if err := rows.Err(); err != nil {
		rows.Close()
		return err
	}
	rows.Close()

	statements := []struct {
		column string
		ddl    string
	}{
		{"normal_x", `ALTER TABLE issues ADD COLUMN normal_x REAL NOT NULL DEFAULT 0`},
		{"normal_y", `ALTER TABLE issues ADD COLUMN normal_y REAL NOT NULL DEFAULT 1`},
		{"normal_z", `ALTER TABLE issues ADD COLUMN normal_z REAL NOT NULL DEFAULT 0`},
		{"reporter", `ALTER TABLE issues ADD COLUMN reporter TEXT NOT NULL DEFAULT ''`},
		{"status_changed_by", `ALTER TABLE issues ADD COLUMN status_changed_by TEXT NOT NULL DEFAULT ''`},
	}
	for _, s := range statements {
		if columns[s.column] {
			continue
		}
		if _, err := database.Exec(s.ddl); err != nil {
			return err
		}
	}
	return nil
}

func (s *Store) Close() error { return s.db.Close() }

func (s *Store) Create(i *Issue) error {
	_, err := s.db.Exec(
		`INSERT INTO issues (id, title, description, priority, status, x, y, z,
		                     normal_x, normal_y, normal_z, reporter, status_changed_by,
		                     created_at, updated_at)
		 VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
		i.ID, i.Title, i.Description, string(i.Priority), string(i.Status),
		i.Position.X, i.Position.Y, i.Position.Z,
		i.Normal.X, i.Normal.Y, i.Normal.Z,
		i.Reporter, i.StatusChangedBy,
		i.CreatedAt, i.UpdatedAt,
	)
	return err
}

func (s *Store) List() ([]Issue, error) {
	rows, err := s.db.Query(
		`SELECT id, title, description, priority, status, x, y, z,
		        normal_x, normal_y, normal_z, reporter, status_changed_by,
		        created_at, updated_at
		 FROM issues ORDER BY created_at DESC, id DESC`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	items := make([]Issue, 0, 16)
	for rows.Next() {
		issue, err := scanIssue(rows)
		if err != nil {
			return nil, err
		}
		items = append(items, *issue)
	}
	return items, rows.Err()
}

func (s *Store) Get(id string) (*Issue, error) {
	row := s.db.QueryRow(
		`SELECT id, title, description, priority, status, x, y, z,
		        normal_x, normal_y, normal_z, reporter, status_changed_by,
		        created_at, updated_at
		 FROM issues WHERE id = ?`, id)
	issue, err := scanIssue(row)
	if errors.Is(err, sql.ErrNoRows) {
		return nil, errIssueNotFound
	}
	return issue, err
}

func (s *Store) UpdateStatus(id string, status Status, updatedAt, changedBy string) (*Issue, error) {
	res, err := s.db.Exec(`UPDATE issues SET status = ?, updated_at = ?, status_changed_by = ? WHERE id = ?`,
		string(status), updatedAt, changedBy, id)
	if err != nil {
		return nil, err
	}
	n, err := res.RowsAffected()
	if err != nil {
		return nil, err
	}
	if n == 0 {
		return nil, errIssueNotFound
	}
	return s.Get(id)
}

func (s *Store) Delete(id string) error {
	res, err := s.db.Exec(`DELETE FROM issues WHERE id = ?`, id)
	if err != nil {
		return err
	}
	n, err := res.RowsAffected()
	if err != nil {
		return err
	}
	if n == 0 {
		return errIssueNotFound
	}
	return nil
}

type scanner interface {
	Scan(dest ...any) error
}

func scanIssue(sc scanner) (*Issue, error) {
	var (
		i        Issue
		priority string
		status   string
	)
	err := sc.Scan(
		&i.ID, &i.Title, &i.Description, &priority, &status,
		&i.Position.X, &i.Position.Y, &i.Position.Z,
		&i.Normal.X, &i.Normal.Y, &i.Normal.Z,
		&i.Reporter, &i.StatusChangedBy,
		&i.CreatedAt, &i.UpdatedAt,
	)
	if err != nil {
		return nil, err
	}
	i.Priority = Priority(priority)
	i.Status = Status(status)
	return &i, nil
}

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
	return &Store{db: db}, nil
}

func (s *Store) Close() error { return s.db.Close() }

func (s *Store) Create(i *Issue) error {
	_, err := s.db.Exec(
		`INSERT INTO issues (id, title, description, priority, status, x, y, z, created_at, updated_at)
		 VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
		i.ID, i.Title, i.Description, string(i.Priority), string(i.Status),
		i.Position.X, i.Position.Y, i.Position.Z, i.CreatedAt, i.UpdatedAt,
	)
	return err
}

func (s *Store) List() ([]Issue, error) {
	rows, err := s.db.Query(
		`SELECT id, title, description, priority, status, x, y, z, created_at, updated_at
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
		`SELECT id, title, description, priority, status, x, y, z, created_at, updated_at
		 FROM issues WHERE id = ?`, id)
	issue, err := scanIssue(row)
	if errors.Is(err, sql.ErrNoRows) {
		return nil, errIssueNotFound
	}
	return issue, err
}

func (s *Store) UpdateStatus(id string, status Status, updatedAt string) (*Issue, error) {
	res, err := s.db.Exec(`UPDATE issues SET status = ?, updated_at = ? WHERE id = ?`,
		string(status), updatedAt, id)
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
		&i.Position.X, &i.Position.Y, &i.Position.Z, &i.CreatedAt, &i.UpdatedAt,
	)
	if err != nil {
		return nil, err
	}
	i.Priority = Priority(priority)
	i.Status = Status(status)
	return &i, nil
}

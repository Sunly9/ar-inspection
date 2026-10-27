package main

import (
	"crypto/rand"
	"encoding/hex"
	"errors"
	"fmt"
	"math"
	"strings"
	"time"
)

// Priority 优先级枚举：low / medium / high
type Priority string

const (
	PriorityLow    Priority = "low"
	PriorityMedium Priority = "medium"
	PriorityHigh   Priority = "high"
)

// Status 处理状态枚举：open / in_progress / resolved
type Status string

const (
	StatusOpen       Status = "open"
	StatusInProgress Status = "in_progress"
	StatusResolved   Status = "resolved"
)

// Position Unity 世界坐标，单位米
type Position struct {
	X float64 `json:"x"`
	Y float64 `json:"y"`
	Z float64 `json:"z"`
}

// Issue 三端共用的数据结构，字段名必须与 Unity / React 完全一致
type Issue struct {
	ID          string   `json:"id"`
	Title       string   `json:"title"`
	Description string   `json:"description"`
	Priority    Priority `json:"priority"`
	Status      Status   `json:"status"`
	Position    Position `json:"position"`
	// Normal 标记贴合处的表面法线，用来还原标记朝向（默认朝上）
	Normal    Position `json:"normal"`
	CreatedAt string   `json:"createdAt"`
	UpdatedAt string   `json:"updatedAt"`
}

// createIssueRequest 只接收客户端该给的字段；id / status / 时间戳一律由服务端生成
type createIssueRequest struct {
	Title       string    `json:"title"`
	Description string    `json:"description"`
	Priority    Priority  `json:"priority"`
	Position    *Position `json:"position"`
	Normal      *Position `json:"normal"`
}

type updateStatusRequest struct {
	Status Status `json:"status"`
}

func (r *createIssueRequest) validate() error {
	r.Title = strings.TrimSpace(r.Title)
	if r.Title == "" {
		return errors.New("title 不能为空")
	}
	if n := len([]rune(r.Title)); n > 100 {
		return fmt.Errorf("title 长度不能超过 100 个字符，当前 %d 个", n)
	}
	if !isValidPriority(r.Priority) {
		return fmt.Errorf("priority 必须是 low、medium 或 high，当前值：%q", string(r.Priority))
	}
	if r.Position == nil {
		return errors.New("position 不能为空")
	}
	if err := r.Position.validate(); err != nil {
		return err
	}

	// normal 可选：不传就当作“朝上”，传了必须合法
	if r.Normal != nil {
		if err := r.Normal.validate(); err != nil {
			return err
		}
	}
	return nil
}

func (p *Position) validate() error {
	for name, v := range map[string]float64{"x": p.X, "y": p.Y, "z": p.Z} {
		if math.IsNaN(v) || math.IsInf(v, 0) || math.Abs(v) > 1000 {
			return fmt.Errorf("position.%s 不是合法坐标", name)
		}
	}
	return nil
}

func isValidPriority(p Priority) bool {
	switch p {
	case PriorityLow, PriorityMedium, PriorityHigh:
		return true
	}
	return false
}

func isValidStatus(s Status) bool {
	switch s {
	case StatusOpen, StatusInProgress, StatusResolved:
		return true
	}
	return false
}

func nowUTC() string {
	return time.Now().UTC().Format(time.RFC3339)
}

// newID 生成 32 位十六进制唯一 ID
func newID() string {
	b := make([]byte, 16)
	if _, err := rand.Read(b); err != nil {
		return fmt.Sprintf("issue-%d", time.Now().UnixNano())
	}
	return hex.EncodeToString(b)
}

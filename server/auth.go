package main

import (
	"crypto/rand"
	"crypto/sha256"
	"encoding/hex"
	"errors"
	"sync"
	"time"
)

// Role 角色：巡检员只能上报，管理员可以改状态和删除
type Role string

const (
	RoleInspector Role = "inspector"
	RoleAdmin     Role = "admin"
)

// User 是返回给客户端的用户信息（不含密码相关字段）
type User struct {
	Username    string `json:"username"`
	DisplayName string `json:"displayName"`
	Role        Role   `json:"role"`

	salt string
	hash string
}

type session struct {
	username string
	role     Role
	expires  time.Time
}

// Auth 是最小实现：账号内置在内存里，登录后发一个随机 token，8 小时后过期。
// 原型不引入用户管理 UI，也不把 users 落库——README 里有说明。
type Auth struct {
	mu       sync.RWMutex
	users    map[string]User
	sessions map[string]session
	ttl      time.Duration
}

func newAuth() *Auth {
	a := &Auth{
		users:    make(map[string]User, 4),
		sessions: make(map[string]session, 16),
		ttl:      8 * time.Hour,
	}
	// 演示账号：巡检员 / 管理员
	a.seed("inspector", "inspector123", "张工（巡检员）", RoleInspector)
	a.seed("admin", "admin123", "李工（管理员）", RoleAdmin)
	return a
}

func (a *Auth) seed(username, password, displayName string, role Role) {
	salt := randomHex(8)
	a.users[username] = User{
		Username:    username,
		DisplayName: displayName,
		Role:        role,
		salt:        salt,
		hash:        hashPassword(salt, password),
	}
}

func hashPassword(salt, password string) string {
	sum := sha256.Sum256([]byte(salt + ":" + password))
	return hex.EncodeToString(sum[:])
}

func randomHex(n int) string {
	b := make([]byte, n)
	if _, err := rand.Read(b); err != nil {
		return "0123456789abcdef0123456789abcdef" //
	}
	return hex.EncodeToString(b)
}

// Login 校验账号密码，成功返回 token 与用户信息
func (a *Auth) Login(username, password string) (string, User, error) {
	a.mu.RLock()
	u, ok := a.users[username]
	a.mu.RUnlock()
	if !ok || u.hash != hashPassword(u.salt, password) {
		return "", User{}, errors.New("账号或密码不正确")
	}

	token := randomHex(24)
	a.mu.Lock()
	a.sessions[token] = session{username: u.Username, role: u.Role, expires: time.Now().Add(a.ttl)}
	a.mu.Unlock()
	return token, u, nil
}

// Lookup 校验 token 是否有效
func (a *Auth) Lookup(token string) (User, bool) {
	if token == "" {
		return User{}, false
	}

	a.mu.RLock()
	s, ok := a.sessions[token]
	a.mu.RUnlock()
	if !ok || time.Now().After(s.expires) {
		return User{}, false
	}

	a.mu.RLock()
	u, exists := a.users[s.username]
	a.mu.RUnlock()
	return u, exists
}

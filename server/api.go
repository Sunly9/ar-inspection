package main

import (
	"encoding/json"
	"errors"
	"log"
	"net/http"
	"strings"
	"time"
)

type api struct {
	store *Store
}

// 统一响应：成功 {"data": ...}；失败 {"error": {"code","message"}}
func writeJSON(w http.ResponseWriter, status int, payload any) {
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.WriteHeader(status)
	if err := json.NewEncoder(w).Encode(payload); err != nil {
		log.Printf("写响应失败: %v", err)
	}
}

func writeData(w http.ResponseWriter, status int, data any) {
	writeJSON(w, status, map[string]any{"data": data})
}

func writeError(w http.ResponseWriter, status int, code, message string) {
	writeJSON(w, status, map[string]any{
		"error": map[string]string{"code": code, "message": message},
	})
}

// GET /api/health
func (a *api) handleHealth(w http.ResponseWriter, r *http.Request) {
	writeData(w, http.StatusOK, map[string]string{"status": "ok", "time": nowUTC()})
}

// POST /api/issues
func (a *api) handleCreateIssue(w http.ResponseWriter, r *http.Request) {
	r.Body = http.MaxBytesReader(w, r.Body, 1<<20)

	var req createIssueRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "INVALID_JSON", "请求体不是合法 JSON")
		return
	}
	if err := req.validate(); err != nil {
		writeError(w, http.StatusBadRequest, "INVALID_ARGUMENT", err.Error())
		return
	}

	ts := nowUTC()
	issue := &Issue{
		ID:          newID(),
		Title:       req.Title,
		Description: strings.TrimSpace(req.Description),
		Priority:    req.Priority,
		Status:      StatusOpen,
		Position:    *req.Position,
		CreatedAt:   ts,
		UpdatedAt:   ts,
	}
	if err := a.store.Create(issue); err != nil {
		log.Printf("保存问题失败: %v", err)
		writeError(w, http.StatusInternalServerError, "INTERNAL", "保存问题失败")
		return
	}
	writeData(w, http.StatusCreated, issue)
}

// GET /api/issues —— 返回对象而不是裸数组，方便 Unity 的 JsonUtility 解析
func (a *api) handleListIssues(w http.ResponseWriter, r *http.Request) {
	items, err := a.store.List()
	if err != nil {
		log.Printf("查询问题列表失败: %v", err)
		writeError(w, http.StatusInternalServerError, "INTERNAL", "查询问题列表失败")
		return
	}
	writeData(w, http.StatusOK, map[string]any{"items": items})
}

// GET /api/issues/{id}
func (a *api) handleGetIssue(w http.ResponseWriter, r *http.Request) {
	issue, err := a.store.Get(r.PathValue("id"))
	if errors.Is(err, errIssueNotFound) {
		writeError(w, http.StatusNotFound, "NOT_FOUND", "问题不存在")
		return
	}
	if err != nil {
		log.Printf("查询问题失败: %v", err)
		writeError(w, http.StatusInternalServerError, "INTERNAL", "查询问题失败")
		return
	}
	writeData(w, http.StatusOK, issue)
}

// PATCH /api/issues/{id}/status
func (a *api) handleUpdateStatus(w http.ResponseWriter, r *http.Request) {
	r.Body = http.MaxBytesReader(w, r.Body, 1<<20)

	var req updateStatusRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, "INVALID_JSON", "请求体不是合法 JSON")
		return
	}
	if !isValidStatus(req.Status) {
		writeError(w, http.StatusBadRequest, "INVALID_ARGUMENT",
			"status 必须是 open、in_progress 或 resolved，当前值："+string(req.Status))
		return
	}

	issue, err := a.store.UpdateStatus(r.PathValue("id"), req.Status, nowUTC())
	if errors.Is(err, errIssueNotFound) {
		writeError(w, http.StatusNotFound, "NOT_FOUND", "问题不存在")
		return
	}
	if err != nil {
		log.Printf("更新状态失败: %v", err)
		writeError(w, http.StatusInternalServerError, "INTERNAL", "更新状态失败")
		return
	}
	writeData(w, http.StatusOK, issue)
}

func (a *api) handleNotFound(w http.ResponseWriter, r *http.Request) {
	writeError(w, http.StatusNotFound, "NOT_FOUND", "接口不存在: "+r.Method+" "+r.URL.Path)
}

// DELETE /api/issues/{id}
func (a *api) handleDeleteIssue(w http.ResponseWriter, r *http.Request) {
	id := r.PathValue("id")

	err := a.store.Delete(id)
	if errors.Is(err, errIssueNotFound) {
		writeError(w, http.StatusNotFound, "NOT_FOUND", "问题不存在")
		return
	}
	if err != nil {
		log.Printf("删除问题失败: %v", err)
		writeError(w, http.StatusInternalServerError, "INTERNAL", "删除问题失败")
		return
	}
	writeData(w, http.StatusOK, map[string]string{"id": id})
}

// ---------- 中间件 ----------

func withMiddleware(next http.Handler) http.Handler {
	return withRecover(withLogging(withCORS(next)))
}

func withCORS(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		h := w.Header()
		h.Set("Access-Control-Allow-Origin", "*")
		h.Set("Access-Control-Allow-Methods", "GET, POST, PATCH, DELETE, OPTIONS")
		h.Set("Access-Control-Allow-Headers", "Content-Type")
		h.Set("Access-Control-Max-Age", "600")
		if r.Method == http.MethodOptions {
			w.WriteHeader(http.StatusNoContent)
			return
		}
		next.ServeHTTP(w, r)
	})
}

type statusRecorder struct {
	http.ResponseWriter
	status int
}

func (s *statusRecorder) WriteHeader(code int) {
	s.status = code
	s.ResponseWriter.WriteHeader(code)
}

func withLogging(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		start := time.Now()
		rec := &statusRecorder{ResponseWriter: w, status: http.StatusOK}
		next.ServeHTTP(rec, r)
		log.Printf("%s %s -> %d (%s)", r.Method, r.URL.Path, rec.status, time.Since(start).Round(time.Millisecond))
	})
}

func withRecover(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		defer func() {
			if v := recover(); v != nil {
				log.Printf("panic: %v", v)
				writeError(w, http.StatusInternalServerError, "INTERNAL", "服务器内部错误")
			}
		}()
		next.ServeHTTP(w, r)
	})
}

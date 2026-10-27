package main

import (
	"context"
	"errors"
	"flag"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"
)

func main() {
	addr := flag.String("addr", "127.0.0.1:8080", "监听地址，真机联调可改成 0.0.0.0:8080")
	dbPath := flag.String("db", "data/issues.db", "SQLite 数据文件路径")
	flag.Parse()

	store, err := openStore(*dbPath)
	if err != nil {
		log.Fatalf("打开数据库失败: %v", err)
	}
	defer store.Close()

	a := &api{store: store, auth: newAuth()}

	mux := http.NewServeMux()
	mux.HandleFunc("GET /api/health", a.handleHealth)
	mux.HandleFunc("POST /api/login", a.handleLogin)

	// 上报：巡检员和管理员都可以
	mux.Handle("POST /api/issues", a.require(RoleInspector, RoleAdmin)(http.HandlerFunc(a.handleCreateIssue)))
	// 查询：登录即可（Unity 端也要登录后再恢复标记）
	mux.Handle("GET /api/issues", a.require()(http.HandlerFunc(a.handleListIssues)))
	mux.Handle("GET /api/issues/{id}", a.require()(http.HandlerFunc(a.handleGetIssue)))
	// 改状态 / 删除：仅管理员
	mux.Handle("PATCH /api/issues/{id}/status", a.require(RoleAdmin)(http.HandlerFunc(a.handleUpdateStatus)))
	mux.Handle("DELETE /api/issues/{id}", a.require(RoleAdmin)(http.HandlerFunc(a.handleDeleteIssue)))

	mux.HandleFunc("/", a.handleNotFound)

	srv := &http.Server{
		Addr:              *addr,
		Handler:           withMiddleware(mux),
		ReadHeaderTimeout: 5 * time.Second,
		WriteTimeout:      15 * time.Second,
		IdleTimeout:       60 * time.Second,
	}

	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()

	go func() {
		log.Printf("巡检问题后端已启动: http://%s  (数据库: %s)", *addr, *dbPath)
		if err := srv.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
			log.Fatalf("服务启动失败: %v", err)
		}
	}()

	<-ctx.Done()
	log.Println("正在关闭服务…")
	shutdownCtx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()
	if err := srv.Shutdown(shutdownCtx); err != nil {
		log.Printf("关闭时出错: %v", err)
	}
	log.Println("已退出")
}

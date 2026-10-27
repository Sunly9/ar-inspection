@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion
title 巡检后端 (Go) - http://127.0.0.1:8080
cd /d "%~dp0server"

rem 端口被占用时，询问是否结束旧进程（常见于改完代码忘记关旧窗口）
for /f "tokens=5" %%p in ('netstat -ano ^| findstr /r /c:":8080 .*LISTENING"') do set "OLDPID=%%p"
if defined OLDPID (
    echo [提示] 8080 端口被进程 !OLDPID! 占用，可能是上一次启动的后端。
    choice /c YN /n /m "是否结束它并继续启动? [Y=结束 / N=退出] "
    if errorlevel 2 (
        echo 已取消。
        echo.
        pause
        exit /b 0
    )
    taskkill /f /pid !OLDPID! >nul 2>nul
    timeout /t 1 /nobreak >nul
    echo 已结束旧进程 !OLDPID!。
    echo.
)

echo 健康检查: http://127.0.0.1:8080/api/health
echo 登录接口: POST http://127.0.0.1:8080/api/login
echo 关闭本窗口或按 Ctrl+C 即可停止服务。
echo.

if exist "inspection-server.exe" (
    echo 使用已编译的 inspection-server.exe 启动 ...
    echo.
    inspection-server.exe -addr 127.0.0.1:8080 -db data/issues.db
    goto :end
)

where go >nul 2>nul
if errorlevel 1 (
    echo [错误] 没找到 go 命令，也没有 inspection-server.exe。
    echo        装好 Go 之后重试，或先运行 build-server.bat 编译出 exe。
    echo.
    pause
    exit /b 1
)

echo 用 go run 启动（首次会编译，约十几秒）...
echo.
go run . -addr 127.0.0.1:8080 -db data/issues.db

:end
echo.
echo 后端已退出。
pause

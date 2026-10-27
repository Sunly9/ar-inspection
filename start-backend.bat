@echo off
chcp 65001 >nul
setlocal
title 巡检后端 (Go) - http://127.0.0.1:8080
cd /d "%~dp0server"

netstat -ano | findstr /r /c:":8080 .*LISTENING" >nul 2>nul
if not errorlevel 1 (
    echo [提示] 8080 端口已被占用，后端可能已经在运行。
    echo        先访问 http://127.0.0.1:8080/api/health 确认；
    echo        如果确认是旧进程，关掉它再运行本脚本。
    echo.
    pause
    exit /b 0
)

echo.
echo 健康检查: http://127.0.0.1:8080/api/health
echo 问题列表: http://127.0.0.1:8080/api/issues
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

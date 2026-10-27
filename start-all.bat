@echo off
chcp 65001 >nul
title 巡检项目 - 一键启动
echo 正在启动后端（新窗口）...
start "巡检后端" cmd /k call "%~dp0start-backend.bat"
timeout /t 3 /nobreak >nul
echo 正在启动管理端（新窗口）...
start "巡检管理端" cmd /k call "%~dp0start-web.bat"
echo.
echo 两个窗口已打开：
echo   后端  http://127.0.0.1:8080/api/health
echo   前端  http://127.0.0.1:5173
echo.
echo 关掉对应窗口即可停止服务。
timeout /t 5 /nobreak >nul

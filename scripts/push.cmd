@echo off

REM Move from scripts folder to repo root
cd /d "%~dp0.."

echo Pulling latest changes from GitHub...
git pull origin main --no-edit

echo Adding files...
git add -A

echo Committing...
git commit -m "update"

echo Pushing to GitHub...
git push origin main

echo Done!
pause
@echo off

echo Pulling latest changes from GitHub...
git pull origin main

echo Adding files...
git add .

echo Committing...
git commit -m "update"

echo Pushing to GitHub...
git push origin main

echo Done!
pause
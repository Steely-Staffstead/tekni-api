@echo off

echo Adding files...
git add .

echo Committing...
git commit -m "update"

echo Pushing to GitHub...
git push

echo Done!
pause
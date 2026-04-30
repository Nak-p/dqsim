param(
    [string]$BuildPath = "Builds\WebGL"
)

# ビルドフォルダの確認
if (-not (Test-Path $BuildPath)) {
    Write-Host "ERROR: ビルドフォルダが見つかりません: $BuildPath" -ForegroundColor Red
    Write-Host "Unity で WebGL ビルドを実行してから再度試してください。"
    Write-Host "出力先: プロジェクトフォルダ内の Builds\WebGL"
    exit 1
}
if (-not (Test-Path "$BuildPath\index.html")) {
    Write-Host "ERROR: index.html が見つかりません。WebGL ビルドか確認してください。" -ForegroundColor Red
    exit 1
}

Write-Host "WebGL ビルドをデプロイします: $BuildPath" -ForegroundColor Cyan

$worktreePath = ".deploy-tmp"

# 既存の worktree を削除
if (Test-Path $worktreePath) {
    git worktree remove $worktreePath --force 2>$null
    Remove-Item $worktreePath -Recurse -Force -ErrorAction SilentlyContinue
}

# gh-pages ブランチの有無を確認
$branchExists = git ls-remote --heads origin gh-pages 2>$null

if ($branchExists) {
    git worktree add $worktreePath gh-pages
} else {
    Write-Host "gh-pages ブランチを新規作成します..."
    git worktree add --orphan -b gh-pages $worktreePath
}

# ビルドファイルをコピー
Write-Host "ファイルをコピー中..."
Get-ChildItem $worktreePath | Where-Object { $_.Name -ne ".git" } | Remove-Item -Recurse -Force
Copy-Item "$BuildPath\*" $worktreePath -Recurse -Force

# コミット＆プッシュ
Push-Location $worktreePath
git add -A
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm"
git commit -m "Deploy WebGL build - $timestamp"
git push origin gh-pages
Pop-Location

# 後片付け
git worktree remove $worktreePath --force
Remove-Item $worktreePath -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "デプロイ完了!" -ForegroundColor Green
Write-Host "アクセスURL: https://nak-p.github.io/dqsim/" -ForegroundColor Green

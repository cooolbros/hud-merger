# New Release
# New-Release.ps1
# Revan
# 16/06/2021

# Get Env
$environment = @{}
Get-Content ".\.env" | ForEach-Object {
	$tokens = $_.Split("=")
	$environment[$tokens[0]] = $tokens[1]
}

# Validate Keys
("GITHUB_REPOSITORY_NAME", "GITHUB_REPOSITORY_URL", "GITHUB_PERSONAL_ACCESS_TOKEN") | ForEach-Object -Process {
	if (!$environment.ContainsKey($_)) {
		throw ".env does not contain $_!

		.env file should resemble:

		GITHUB_REPOSITORY_NAME=My Repository
		GITHUB_REPOSITORY_URL=https://github.com/user/my-repository
		GITHUB_PERSONAL_ACCESS_TOKEN=ghp_abcdefghijklmnopqrstuvwxyz1234567890"
	}
}

$repo = $environment.GITHUB_REPOSITORY_NAME
$personal_access_token = $environment.GITHUB_PERSONAL_ACCESS_TOKEN
$github_repository_url = $environment.GITHUB_REPOSITORY_URL

$repository_information = [Uri]::New($github_repository_url).AbsolutePath.Split("/")
$author = $repository_information[1]
$id = $repository_information[2]

$authorization = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("$($author):$($personal_access_token)"))

# Get latest release
Write-Host "Getting release info for $id"
$latestRelease = (ConvertFrom-Json (Invoke-WebRequest "https://api.github.com/repos/$($author)/$($id)/releases/latest"))
Write-Host $latestRelease

# Create version
$newVersion
if ($latestRelease.message -eq "Not Found") {
	# First release
	$newVersion = "1.0.0"
}
else {
	# Increment Version
	$tags = $latestRelease.tag_name.Split(".") | ForEach-Object { [int]::Parse($_) }
	$tags[2]++
	if ($tags[2] -eq 10) {
		$tags[2] = 0
		$tags[1]++
		if ($tags[1] -eq 10) {
			$tags[1] = 0
			$tags[0]++
		}
	}
	$newVersion = "$($tags[0]).$($tags[1]).$($tags[2])"
}

# Update version in project

$csprojPath = (Get-ChildItem -Recurse *.csproj).FullName
$csproj = [xml](Get-Content $csprojPath)
$csproj.Project.PropertyGroup.Version = $newVersion
$csproj.Save($csprojPath)

$aboutWindowPath = $id + "\\AboutWindow.xaml"
$aboutWindow = [xml](Get-Content $aboutWindowPath)
$aboutWindow.Window.Grid.StackPanel.Label[1] = "Version " + $newVersion
$aboutWindow.Save($aboutWindowPath)

# Push changes (Ensure 0 commits to main since this release)
git add .
git commit -m "Update version number"
git push

# Restore
Write-Host "Restoring $id"
Remove-Item -Recurse "$id/bin" -ErrorAction "SilentlyContinue"
Remove-Item -Recurse "$id/obj" -ErrorAction "SilentlyContinue"
Remove-Item "$id.zip" -ErrorAction "SilentlyContinue"

# Build
Write-Host "Building $id"
# https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish
# https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
dotnet publish hud-merger -c Release
# -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true --runtime win-x64

# Package
Write-Host "Packaging $id"
$output = "$id\bin\Release\net5.0-windows\"
Rename-Item "$output\publish" "$id"
Compress-Archive -Path "$output\hud-merger" -Destination "./$id.zip" -CompressionLevel "Optimal"


# Create Changelog
Write-Host "Writing changelog $($latestRelease.published_at)"
$changelog = ""
git log --after $latestRelease.published_at --format=%s | ForEach-Object { $changelog += " - $_`r`n" }

# Create Release
Write-Host "Creating release $($repo) $($newVersion)"

$params = @{
	Uri     = "https://api.github.com/repos/$($author)/$($id)/releases"
	Method  = "POST"
	Headers = @{
		Authorization = "Basic $authorization"
		Accept        = "application/vnd.github.v3+json"
	}
	Body    = (ConvertTo-Json @{
			owner    = $author
			repo     = $id
			name     = "$($repo) $($newVersion)"
			tag_name = $newVersion
			body     = "# $($repo) $($newVersion)`r`n`r`n$changelog"
			draft    = $args[0] == "draft"
			prerelease = $args[0] == "prerelease"
		})
}

$release = ConvertFrom-Json -InputObject (Invoke-WebRequest @params)
Write-Host $release

# Add ZIP to release
Write-Host "Uploading $id.zip to release $($release.id)"

$params = @{
	Uri     = "https://uploads.github.com/repos/$author/$id/releases/$($release.id)/assets?name=$id.zip"
	Method  = "POST"
	Headers = @{
		"Content-Type" = "application/zip"
		Accept         = "application/vnd.github.v3+json"
		Authorization  = "Basic $authorization)"
	}
	Body    = ([System.IO.File]::ReadAllBytes(("$((Get-Location).Path)\$id.zip")))
}

$upload = ConvertFrom-Json -InputObject (Invoke-WebRequest @params)
Write-Host $upload

Write-Host "Removing $id.zip"
Remove-Item "$id.zip"
# CI/CD Workflow Documentation

## Overview

This repository contains a CI/CD pipeline using GitHub Actions to automate the build, testing, and release of the application.

The goal of this workflow is to ensure version control, build consistency, and simplified release management.

---

## Workflow Trigger

The workflow is triggered when a Git tag following Semantic Versioning (SemVer) is created and pushed.

Examples of valid tags:
* `v1.0.0` (stable release)
* `v1.1.0-rc.1` (release candidate)

---

## Workflow Steps

1.  **Checkout Code**
    * Retrieves the repository source code.

2.  **Setup .NET**
    * Installs the required .NET SDK (`8.0.x`) to compile the project.

3.  **Restore Dependencies**
    * Runs `dotnet restore` to download and prepare dependencies.

4.  **Build Project**
    * Builds the application in `Release` mode for the target runtime (`win-x64`).

5.  **Publish Artifacts**
    * Publishes the compiled application and makes it available as a GitHub Actions artifact.

---

## Usage Instructions

### Creating a Release

1.  Ensure all changes are committed and pushed to the repository.
2.  Create a version tag following the SemVer convention:
    ```bash
    git tag v1.0.0
    git push origin v1.0.0
    ```
    or

    ```bash
    git tag v1.1.0-rc.1
    git push origin v1.1.0-rc.1
    ```
3.  This will automatically trigger the workflow, build the project, and generate release artifacts.

### Accessing Build Artifacts

After the workflow finishes, the generated files can be downloaded from the GitHub Actions artifacts section in the workflow run.

---

## Best Practices

* Use pre-release tags (`rc.1`, `beta.2`, `alpha.3`, `preview.4`, etc.) for testing before making a final release.
* Always increment versions according to SemVer rules:
    * **MAJOR**: Breaking changes
    * **MINOR**: New features without breaking changes
    * **PATCH**: Bug fixes


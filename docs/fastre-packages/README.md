# Fastre Packages

FADE comes with a package manager that allows you to install and manage Fastre packages. Fastre packages are collections of reusable components, styles, and scripts that you can use in your web applications. This directory contains instructions for installing and updating Fastre packages using the FADE package manager.

> [!NOTE]
> As of now, Fastre only supports packages on global scope. Hence, local packages are not supported.

## Installing Packages

To install a Fastre package, use the following command:

```bash
fastre install <author>/<package>
```

This command will download and install the specified package from GitHub. Please note that the package must be hosted on GitHub for this command to work. Make sure to not install random packages for security reasons.

The GitHub repository for the package should contain a `pkg.json` file that describes the package and its contents. The package manager will use this file to install the package and its dependencies.

The repository should also contain release tags that correspond to the versions of the package. The package manager will install the latest release tag by default.

## Updating Packages

To update a Fastre package, use the following command:

```bash
fastre update <author>/<package>
```

This command will check for updates to the specified package (through release tags on GitHub) and install the latest version if available.

## Removing Packages

To remove a Fastre package, use the following command:

```bash
fastre remove <author>/<package>
```

This command will remove the specified package from your fastre installation.



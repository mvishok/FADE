# Installnation

## Download FADE

FADE installers are available in the [Releases](https://github.com/mvishok/fade/releases) section. You can download the latest version of FADE for your operating system from the releases page.

A direct link to the latest version of FADE for Windows is available on the [homepage](https://fade.vishok.me/) of the FADE repository.

## Install FADE

### Windows

- Download the latest version of FADE from the above link based on your operating system
- Run the downloaded setup file
- Follow the on-screen instructions to install FADE

FADE is currently available for Windows only. Support for other operating systems will be added in future releases.

### Compiling from Source

FADE is built using C# and .NET. You can compile the source code to create an executable for your operating system. The source code is available in the [root](https://github.com/mvishok/fade/tree/main) directory of the FADE repository.

To compile the source code, you will need to install the [.NET SDK](https://dotnet.microsoft.com/download) on your system. Once you have installed the SDK, you can use the `dotnet build` command to compile the source code.

## Install Packages

As the name suggests, FADE is a development environment that includes Fastre and Autobasee. To install these packages, use the following commands:

>[!NOTE]
> The following commands require administrative privileges. Run them in an elevated command prompt (Run as Administrator).

```bash
fade install fastre
fade install autobasee
```

These commands will download and install the latest versions of Fastre and Autobasee from the official package repository.


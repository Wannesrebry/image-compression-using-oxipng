# Image Compression Using Oxipng

This project provides a .NET web API for compressing PNG images using the [Oxipng](https://github.com/shssoichiro/oxipng) command-line tool.

## How It Works

1. The API receives a PNG image upload request.
2. The backend saves the image and invokes the Oxipng binary to compress it.
3. The compressed image is returned to the user.

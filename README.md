# OpenImageProcessor
OpenImageProcessor is DNN wrapper library around ImageProcessor Library For On-The-Fly Processing Of Images.

##ImageProcessor
ImageProcessor is a collection of lightweight libraries that allows you to manipulate images on-the-fly

It include a dynamic image processing extension built for ASP.NET.

It’s lighting fast, extensible, easy to use, comes bundled with some great features and is fully open source.

More info  [imageprocessor.org](http://imageprocessor.org/)

It can help you to resize, crop, watermark, and much more images on the fly by only adding query parameters on image urls.

Exemple : http://your-image?width=600&height=250&bgcolor=fff

Documentation  :  [imageprocessor.org/imageprocessor-web/imageprocessingmodule/](http://imageprocessor.org/imageprocessor-web/imageprocessingmodule/)

Look at the methods sections

OpenImageProcessor do nothing more then install the ImageProcessor library and apply automatically the needed configuration in your DNN website.

In detail :
* it install the dll's in the bin folder
* make the needed modifications in the web.config
* add the 3 config files in the config sub folder

## Requirements
 .NET 4.5+

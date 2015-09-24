// TODO: Support dirty rendering.
var protocol = (function () {
  var image = null;
  var imageData = null;

  return {
    render: function(canvas, context) {
      context.putImageData(image, 0, 0);
    },

    start: function (binaryReader, canvas, context) {
      canvas.width = binaryReader.readUnsignedShort();
      canvas.height = binaryReader.readUnsignedShort();
      image = context.createImageData(canvas.width, canvas.height);
      imageData = image.data;

      for (var index = 0; !binaryReader.isEndOfBuffer() ; index += 4) {
        imageData[index + 2] = binaryReader.readUnsignedByte();
        imageData[index + 1] = binaryReader.readUnsignedByte();
        imageData[index + 0] = binaryReader.readUnsignedByte();
        imageData[index + 3] = 255;
      }
    },

    update: function (binaryReader, canvas, context) {
      while (!binaryReader.isEndOfBuffer()) {
        var index = binaryReader.readUnsignedInteger();
        imageData[index + 2] = binaryReader.readUnsignedByte();
        imageData[index + 1] = binaryReader.readUnsignedByte();
        imageData[index + 0] = binaryReader.readUnsignedByte();
      }
    }
  };
})();
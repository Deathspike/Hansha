// TODO: Implement dirty rendering support. Currently just overwriting the entire image, which is bad.
var protocol = (function () {
  var image = null;

  return {
    render: function(canvas, context) {
      context.putImageData(image, 0, 0);
    },

    start: function (binaryReader, canvas, context) {
      canvas.width = binaryReader.readUnsignedShort();
      canvas.height = binaryReader.readUnsignedShort();
      image = context.createImageData(canvas.width, canvas.height);

      for (var p = 0; !binaryReader.isEndOfBuffer() ; p += 4) {
        image.data[p + 2] = binaryReader.readUnsignedByte();
        image.data[p + 1] = binaryReader.readUnsignedByte();
        image.data[p + 0] = binaryReader.readUnsignedByte();
        image.data[p + 3] = 255;
      }
    },

    update: function (binaryReader, canvas, context) {
      while (!binaryReader.isEndOfBuffer()) {
        var p = binaryReader.readUnsignedInteger();
        image.data[p + 2] = binaryReader.readUnsignedByte();
        image.data[p + 1] = binaryReader.readUnsignedByte();
        image.data[p + 0] = binaryReader.readUnsignedByte();
      }
    }
  };
})();
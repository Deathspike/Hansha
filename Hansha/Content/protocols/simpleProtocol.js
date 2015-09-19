// TODO: Implement dirty rendering support. Currently just overwriting the entire image, which is bad.
var protocol = (function () {
  var image = null;
  var isDirty = false;

  return {
    render: function(canvas, context) {
      if (isDirty) {
        context.putImageData(image, 0, 0);
        isDirty = false;
      }
    },

    start: function(buffer, canvas, context) {
      var reader = new BinaryReader(buffer);

      canvas.width = reader.readUnsignedShort();
      canvas.height = reader.readUnsignedShort();
      image = context.createImageData(canvas.width, canvas.height);

      for (var p = 0; !reader.isEndOfBuffer(); p += 4) {
        image.data[p + 2] = reader.readUnsignedByte();
        image.data[p + 1] = reader.readUnsignedByte();
        image.data[p + 0] = reader.readUnsignedByte();
        image.data[p + 3] = 255;
      }

      isDirty = true;
    },

    update: function (buffer, canvas, context) {
      var reader = new BinaryReader(buffer);

      while (!reader.isEndOfBuffer()) {
        var p = reader.readUnsignedInteger();
        image.data[p + 2] = reader.readUnsignedByte();
        image.data[p + 1] = reader.readUnsignedByte();
        image.data[p + 0] = reader.readUnsignedByte();
      }

      isDirty = true;
    }
  };
})();
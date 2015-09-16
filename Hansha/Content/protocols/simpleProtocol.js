// TODO: Think of dirty region rendering. Overwriting all pixels for no reason at all is a bit meh.
var protocol = (function () {
  var image = null;
  var isDirty = false;

  return {
    render: function(canvas, context) {
      if (isDirty) {
        context.putImageData(image, 0, 0);
      }
    },

    start: function(buffer, canvas, context) {
      var reader = new BinaryReader(buffer);

      canvas.width = reader.readUInt16();
      canvas.height = reader.readUInt16();
      image = context.createImageData(canvas.width, canvas.height);

      for (var p = 0; !reader.isEndOfStream(); p += 4) {
        image.data[p + 2] = reader.readByte();
        image.data[p + 1] = reader.readByte();
        image.data[p + 0] = reader.readByte();
        image.data[p + 3] = 255;
      }

      isDirty = true;
    },

    update: function (buffer, canvas, context) {
      var reader = new BinaryReader(buffer);

      while (!reader.isEndOfStream()) {
        var p = reader.readUInt32();
        image.data[p + 2] = reader.readByte();
        image.data[p + 1] = reader.readByte();
        image.data[p + 0] = reader.readByte();
      }

      isDirty = true;
    }
  };
})();
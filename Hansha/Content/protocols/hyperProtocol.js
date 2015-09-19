// TODO: Implement dirty rendering support. Currently just overwriting the entire image, which is bad.
var protocol = (function () {
  var image = null;
  var screenHeight = 0;
  var screenWidth = 0;

  return {
    render: function (canvas, context) {
      context.putImageData(image, 0, 0);
    },

    start: function (binaryReader, canvas, context) {
      screenWidth = canvas.width = binaryReader.readUnsignedShort();
      screenHeight = canvas.height = binaryReader.readUnsignedShort();
      image = context.createImageData(canvas.width, canvas.height);
      
      for (var index = 0; !binaryReader.isEndOfBuffer(); index += 4) {
        processPixel(binaryReader, index);
      }
    },

    update: function (binaryReader, canvas, context) {
      processMovedRegions(binaryReader);
      processModifiedRegions(binaryReader);
    }
  };
  
  function processMovedRegions(binaryReader) {
    var numberOfMovedRegions = binaryReader.readUnsignedVariableLength();

    for (var i = 0; i < numberOfMovedRegions; i++) {
      var fromX = binaryReader.readUnsignedVariableLength();
      var fromY = binaryReader.readUnsignedVariableLength();
      var toX = binaryReader.readUnsignedVariableLength();
      var toY = binaryReader.readUnsignedVariableLength();
      var rectangleWidth = binaryReader.readUnsignedVariableLength();
      var rectangleHeight = binaryReader.readUnsignedVariableLength();

      for (var yOffset = 0; yOffset < rectangleHeight; yOffset++) {
        var yOrigin = fromY + yOffset;
        var yOriginIndex = yOrigin * screenWidth * 4;
        var yDestination = toY + yOffset;
        var yDestinationIndex = yDestination * screenWidth * 4;

        for (var xOffset = 0; xOffset < rectangleWidth; xOffset++) {
          var xOrigin = fromX + xOffset;
          var xOriginIndex = yOriginIndex + xOrigin * 4;
          var xDestination = toX + xOffset;
          var xDestinationIndex = yDestinationIndex * xDestination * 4;

          image.data[xDestinationIndex + 0] = image.data[xOriginIndex + 0];
          image.data[xDestinationIndex + 1] = image.data[xOriginIndex + 1];
          image.data[xDestinationIndex + 2] = image.data[xOriginIndex + 2];
        }
      }
    }
  }

  function processModifiedRegions(binaryReader) {
    var previousIndex = 0;

    while (!binaryReader.isEndOfBuffer()) {
      var currentIndex = binaryReader.readSignedVariableLength() + previousIndex;
      processPixel(binaryReader, currentIndex);
      previousIndex = currentIndex;
    }
  }

  function processPixel(binaryReader, index) {
    var blueAndGreen = binaryReader.readUnsignedByte();
    var greenAndRed = binaryReader.readUnsignedByte();
    image.data[index + 2] = blueAndGreen & 248;
    image.data[index + 1] = (blueAndGreen & 7) << 5 | (greenAndRed & 7) << 2;
    image.data[index + 0] = greenAndRed & 248;
  }
})();
(function() {
  var canvas = document.getElementById('canvas');
  var context = canvas.getContext('2d');
  var hasStarted = false;
  var socket = new WebSocket('ws://' + window.location.host + '/ws');
  var isDirty = false;

  context.webkitImageSmoothingEnabled = false;
  context.mozImageSmoothEnabled = false;
  context.imageSmoothingEnabled = false;

  socket.binaryType = 'arraybuffer';

  socket.addEventListener('close', function() {
    alert('Connection dropped.');
    location.reload();
  });

  socket.addEventListener('message', function (event) {
    var decompressedBuffer = decompress(event.data);
    var begin = Date.now();
    var binaryReader = new BinaryReader(decompressedBuffer);
    if (!hasStarted) {
      hasStarted = true;
      protocol.start(binaryReader, canvas, context);
      requestDraw();
    } else {
      protocol.update(binaryReader, canvas, context);
      requestDraw();
    }
    console.log('Processed in ' + (Date.now() - begin) + 'ms');
  });

  function decompress(buffer) {
    var begin = Date.now();
    var inflater = new Zlib.RawInflate(new Uint8Array(buffer));
    var decompressedBuffer = inflater.decompress();
    console.log('Decompressed in ' + (Date.now() - begin) + 'ms');
    return decompressedBuffer;
  }

  function draw() {
    if (isDirty) {
      protocol.render(canvas, context);
      isDirty = false;
    }
  }

  function requestDraw() {
    isDirty = true;
    requestAnimationFrame(draw);
  }
})();
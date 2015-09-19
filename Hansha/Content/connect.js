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
    var inflater = new Zlib.RawInflate(new Uint8Array(event.data));
    var binaryReader = new BinaryReader(inflater.decompress());

    if (!hasStarted) {
      hasStarted = true;
      protocol.start(binaryReader, canvas, context);
      requestDraw();
    } else {
      protocol.update(binaryReader, canvas, context);
      requestDraw();
    }
  });

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
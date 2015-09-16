(function() {
  var canvas = document.getElementById('canvas');
  var context = canvas.getContext('2d');
  var hasStarted = false;
  var socket = new WebSocket('ws://' + window.location.host + '/ws');

  context.webkitImageSmoothingEnabled = false;
  context.mozImageSmoothEnabled = false;
  context.imageSmoothingEnabled = false;

  socket.binaryType = 'arraybuffer';

  socket.addEventListener('close', function() {
    alert('Connection dropped.');
    location.reload();
  });

  socket.addEventListener('message', function(event) {
    if (!hasStarted) {
      hasStarted = true;
      protocol.start(event.data, canvas, context);
    } else {
      protocol.update(event.data, canvas, context);
    }
  });

  (function draw() {
    protocol.render(canvas, context);
    requestAnimationFrame(draw);
  })();
})();
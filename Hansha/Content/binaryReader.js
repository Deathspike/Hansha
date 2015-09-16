function BinaryReader(buffer) {
  this._buffer = buffer;
  this._position = 0;
  this._view = new Uint8Array(buffer);
}

BinaryReader.prototype.isEndOfStream = function() {
  return this._position >= this._buffer.byteLength;
};

BinaryReader.prototype.read = function(numberOfBytes) {
  var buffer = [];
  for (var i = 0; i < numberOfBytes; i += 1, this._position += 1) buffer.push(this._view[this._position]);
  return buffer;
};

BinaryReader.prototype.readByte = function() {
  return this.read(1)[0];
};

BinaryReader.prototype.readUInt16 = function() {
  var buffer = this.read(2);
  return buffer[1] << 8 | buffer[0];
};

BinaryReader.prototype.readUInt32 = function() {
  var buffer = this.read(4);
  return buffer[3] << 24 | buffer[2] << 16 | buffer[1] << 8 | buffer[0];
};
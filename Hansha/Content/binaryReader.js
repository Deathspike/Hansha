/**
 * Represents a binary reader.
 * @constructor
 * @param {(Array<number>|TypedArray)} buffer The buffer.
 */
function BinaryReader(buffer) {
  this._buffer = new Uint8Array(buffer);
  this._position = 0;
}

/**
 * Determines whether the position is at the end of the buffer.
 * @return {boolean} Indicates whether the position is at the end of the buffer.
 */
BinaryReader.prototype.isEndOfBuffer = function () {
  return this._position >= this._buffer.byteLength;
};

/**
 * Reads a number of bytes and advances the position.
 * @param {number} count The number of bytes to read.
 * @return {Array<number>} The bytes.
 */
BinaryReader.prototype.readBytes = function (count) {
  var buffer = [];
  for (var i = 0; i < count; i++) {
    if (this._position < this._buffer.byteLength) {
      buffer.push(this._buffer[this._position]);
      this._position++;
    } else {
      buffer.push(0);
    }
  }
  return buffer;
};

/**
 * Reads a signed byte and advances the position.
 * @return {number} The signed byte.
 */
BinaryReader.prototype.readSignedByte = function () {
  return this.readUnsignedByte() | -128;
};

/**
 * Reads a signed integer and advances the position.
 * @return {number} The signed integer.
 */
BinaryReader.prototype.readSignedInteger = function () {
  return this.readUnsignedInteger() | -2147483648;
};

/**
 * Reads an signed short and advances the position.
 * @return {number} The signed short.
 */
BinaryReader.prototype.readSignedShort = function () {
  return this.readUnsignedShort() | -32768;
};

/**
 * Reads a signed LEB128 variable-length number and advances the position.
 * @return {number} The signed LEB128 variable-length number.
 */
BinaryReader.prototype.readSignedVariableLength = function () {
  var currentByte = 0;
  var result = 0;
  var shift = 0;
  while (true) {
    currentByte = this.readUnsignedByte();
    result |= (currentByte & 127) << shift;
    shift += 7;
    if ((currentByte & 128) === 0) {
      break;
    }
  }
  if (currentByte & 64) {
    result |= -(1 << shift);
  }
  return result;
};

/**
 * Reads an signed byte and advances the position.
 * @return {number} The unsigned byte.
 */
BinaryReader.prototype.readUnsignedByte = function () {
  return this.readBytes(1)[0];
};

/**
 * Reads an unsigned integer and advances the position.
 * @return {number} The unsigned integer.
 */
BinaryReader.prototype.readUnsignedInteger = function () {
  var buffer = this.readBytes(4);
  return buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24);
};

/**
 * Reads an unsigned short and advances the position.
 * @return {number} The unsigned short.
 */
BinaryReader.prototype.readUnsignedShort = function () {
  var buffer = this.readBytes(2);
  return buffer[0] | (buffer[1] << 8);
};

/**
 * Reads an unsigned LEB128 variable-length number and advances the position.
 * @return {number} The unsigned LEB128 variable-length number.
 */
BinaryReader.prototype.readUnsignedVariableLength = function () {
  var result = 0;
  var shift = 0;
  while (true) {
    var currentByte = this.readUnsignedByte();
    result |= (currentByte & 127) << shift;
    shift += 7;
    if ((currentByte & 128) === 0) {
      break;
    }
  }
  return result;
};

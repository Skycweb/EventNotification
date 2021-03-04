using System;
namespace RemoteNotificationCommon
{
    public class BytesBuffer
    {
        private byte[] buffer;
        private int readIndex = 0;
        private int writeIndex = 0;
        private int bufferSize;
        public BytesBuffer(int BufferSize)
        {
            this.buffer = new byte[BufferSize];
            this.bufferSize = BufferSize;
        }
        private void resetPoint() {
            if (this.writeIndex < int.MaxValue - this.bufferSize) {
                return;
            }
            this.readIndex = this.readIndex % this.bufferSize;
            this.writeIndex = this.writeIndex % this.bufferSize;
            if (this.readIndex > this.writeIndex) {
                this.writeIndex = this.writeIndex + this.bufferSize;
            }
        }
        public int LenghtOfRead {
            get {
                return this.writeIndex - this.readIndex;
            }
        }
        public int LenghtOfWrite {
            get {
                return this.bufferSize - this.LenghtOfRead;
            }
        }
        public void apped(byte[] data, int index, int len) {
            if (len > this.LenghtOfWrite)
            {
                throw new Exception("增加的数据长度大于缓存池长度");
            }
            this.resetPoint();
            for (int i = index; i < index+len; i++)
            {
                this.buffer[this.writeIndex++] = data[i];
            }
            this.resetPoint();
        }
        public void apped(byte[] data) {
            if (data.Length > this.LenghtOfWrite)
            {
                throw new Exception("增加的数据长度大于缓存池长度");
            }
            for (int i = 0; i < data.Length; i++) {
                this.buffer[this.writeIndex++] = data[i];
            }
            this.resetPoint();
            
            
            
        }
        /// <summary>
        /// 每次都是从read之后重新算起,0起步
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public byte this[int index] {
            get {
                byte data = buffer[(readIndex + index) % bufferSize];
                return data;
            }
        }
        /// <summary>
        ///  向前读取数据
        /// </summary>
        /// <param name="len"></param>
        /// <returns></returns>
        public byte[] read(int len) {
            
            byte[] data = new byte[len];
            for (int i = 0; i < len; i++)
                data[i] = this.buffer[this.readIndex++ % this.bufferSize];
            return data;
        }
    }
}

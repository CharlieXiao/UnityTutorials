﻿using Unity.Mathematics;

namespace UnityTutorials.Pseudorandom_Noise
{
    public readonly struct SmallXXHash
    {
        const uint primeA = 0b10011110001101110111100110110001;
        const uint primeB = 0b10000101111010111100101001110111;
        const uint primeC = 0b11000010101100101010111000111101;
        const uint primeD = 0b00100111110101001110101100101111;
        const uint primeE = 0b00010110010101100110011110110001;

        private readonly uint accumulator;

        public SmallXXHash(uint accumulator)
        {
            this.accumulator = accumulator;
        }
        
        // uint -> SmallXXHash

        public static implicit operator SmallXXHash(uint accumulator) => new SmallXXHash(accumulator);

        // SmallXXHash -> uint
        public static implicit operator uint(SmallXXHash hash)
        {
            uint avalanche = hash.accumulator;
            avalanche ^= avalanche >> 15;
            avalanche *= primeB;
            avalanche ^= avalanche >> 13;
            avalanche *= primeC;
            avalanche ^= avalanche >> 16;
            return avalanche;
        }
        
        // 滚动哈希，将左移的数据放到数据的最后
        // 11001100 << 3 | 11001100 >> 5
        // 01100110
        static uint RotateLeft(uint data, int steps) => (data << steps) | (data >> (32-steps));

        public SmallXXHash Eat(int data) => RotateLeft(accumulator + (uint)data * primeC,17) * primeD;

        public SmallXXHash Eat(byte data) => RotateLeft(accumulator + data * primeE, 11) * primeA;

        public static SmallXXHash Seed(int seed) => (uint)seed + primeE;
    }
    
    public readonly struct SmallXXHash4
    {
        const uint primeB = 0b10000101111010111100101001110111;
        const uint primeC = 0b11000010101100101010111000111101;
        const uint primeD = 0b00100111110101001110101100101111;
        const uint primeE = 0b00010110010101100110011110110001;

        private readonly uint4 accumulator;

        public SmallXXHash4(uint4 accumulator)
        {
            this.accumulator = accumulator;
        }
        
        // uint -> SmallXXHash

        public static implicit operator SmallXXHash4(uint4 accumulator) => new SmallXXHash4(accumulator);

        // SmallXXHash -> uint
        public static implicit operator uint4(SmallXXHash4 hash)
        {
            uint4 avalanche = hash.accumulator;
            avalanche ^= avalanche >> 15;
            avalanche *= primeB;
            avalanche ^= avalanche >> 13;
            avalanche *= primeC;
            avalanche ^= avalanche >> 16;
            return avalanche;
        }
        
        // SmallXXHash -> SmallXXHash4
        public static implicit operator SmallXXHash4(SmallXXHash hash) => new SmallXXHash4((uint)hash);

        public static SmallXXHash4 operator +(SmallXXHash4 hash, int v) => hash.accumulator + (uint)v;
        
        // 滚动哈希，将左移的数据放到数据的最后
        // 11001100 << 3 | 11001100 >> 5
        // 01100110
        static uint4 RotateLeft(uint4 data, int steps) => (data << steps) | (data >> (32-steps));

        public SmallXXHash4 Eat(int4 data) => RotateLeft(accumulator + (uint4)data * primeC,17) * primeD;

        public static SmallXXHash4 Seed(int4 seed) => (uint4)seed + primeE;

        public uint4 BytesA => (uint4)this & 255;

        public uint4 BytesB => ((uint4)this >> 8) & 255;

        public uint4 BytesC => ((uint4)this >> 16) & 255;

        public uint4 BytesD => (uint4)this >> 24;

        public float4 Floats01A => (float4)BytesA * (1f / 255f);

        public float4 Floats01B => (float4)BytesB * (1f / 255f);

        public float4 Floats01C => (float4)BytesC * (1f / 255f);

        public float4 Floats01D => (float4)BytesD * (1f / 255f);

        public uint4 GetBits(int count, int shift) => ((uint4) this >> shift) & (uint) ((1 << count) - 1);

        public float4 GetBitsAsFloats01(int count, int shift) =>
            (float4) GetBits(count, shift) * (1.0f / ((1 << count) - 1));
    }
}
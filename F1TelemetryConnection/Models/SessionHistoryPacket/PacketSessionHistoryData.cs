﻿using F1Telemetry.Helpers;

namespace F1Telemetry.Models.SessionHistoryPacket
{
    public class PacketSessionHistoryData : ProtoModel
    {
        public PacketSessionHistoryData(PacketHeader header, byte[] array)
        {
            this.Header = header;
            this.Index = this.Header.Index;
            this.PickReader(this.Header.PacketFormat, array);
        }

        public PacketHeader Header { get; private set; }
        public byte CarIndex { get; private set; }
        public byte NumberOfLaps { get; private set; }
        public byte NumberOfTyreStints { get; private set; }
        public byte BestLapTimeLapNumber { get; private set; }
        public byte BestSector1LapNumber { get; private set; }
        public byte BestSector2LapNumber { get; private set; }
        public byte BestSector3LapNumber { get; private set; }
        public LapHistoryData[] LapHistoryData { get; private set; }
        public TyreStintHistoryData[] TyreStintsHistoryData { get; private set; }

        protected override void Reader2021(byte[] array)
        {
            int index = this.Index;
            byte valb;

            //PacketHeader m_header;                   // Header
            //uint8 m_carIdx;                   // Index of the car this lap data relates to
            index += ByteReader.ToUInt8(array, index, out valb);
            this.CarIndex = valb;

            //uint8 m_numLaps;                  // Num laps in the data (including current partial lap)
            index += ByteReader.ToUInt8(array, index, out valb);
            this.NumberOfLaps = valb;

            //uint8 m_numTyreStints;            // Number of tyre stints in the data
            index += ByteReader.ToUInt8(array, index, out valb);
            this.NumberOfTyreStints = valb;

            //uint8 m_bestLapTimeLapNum;        // Lap the best lap time was achieved on
            index += ByteReader.ToUInt8(array, index, out valb);
            this.BestLapTimeLapNumber = valb;

            //uint8 m_bestSector1LapNum;        // Lap the best Sector 1 time was achieved on
            index += ByteReader.ToUInt8(array, index, out valb);
            this.BestSector1LapNumber = valb;

            //uint8 m_bestSector2LapNum;        // Lap the best Sector 2 time was achieved on
            index += ByteReader.ToUInt8(array, index, out valb);
            this.BestSector2LapNumber = valb;

            //uint8 m_bestSector3LapNum;        // Lap the best Sector 3 time was achieved on
            index += ByteReader.ToUInt8(array, index, out valb);
            this.BestSector3LapNumber = valb;

            //LapHistoryData m_lapHistoryData[100];   // 100 laps of data max
            this.LapHistoryData = new LapHistoryData[100];
            for (int i = 0; i < this.LapHistoryData.Length; i++)
            {
                this.LapHistoryData[i] = new LapHistoryData(index, this.Header.PacketFormat, array);
                index = this.LapHistoryData[i].Index;
            }

            //TyreStintHistoryData m_tyreStintsHistoryData[8];
            this.TyreStintsHistoryData = new TyreStintHistoryData[8];
            for (int i = 0; i < this.TyreStintsHistoryData.Length; i++)
            {
                this.TyreStintsHistoryData[i] = new TyreStintHistoryData(index, this.Header.PacketFormat, array);
                index = this.TyreStintsHistoryData[i].Index;
            }

            this.Index = index;
        }
    }
}
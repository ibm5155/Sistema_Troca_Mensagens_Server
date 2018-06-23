using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatApplication
{

    public enum DataIdentifier
    {
        NoPacket,
        WrongPassw,
        DuplicateId,
        Message,
        UpdateList,
        OK,
        LogIn,
        LogOut,
        Null,
        ELECTION,
        Coordinator
    }

    public class Packet
    {
        #region Private Members
        #endregion

        #region Public Properties
        public Dictionary<string, Object> ReadData = new Dictionary<string, object>();
        public DataIdentifier GetDataIdentifier
        {
            get
            {
                var read = ReadData["ChatDataIdentifier"];
                if (read != null)
                {
                    return (DataIdentifier)Enum.Parse(typeof(DataIdentifier), read.ToString());
                }
                return DataIdentifier.NoPacket;
            }
        }
        #endregion

        #region Methods

        // Default Constructor
        public Packet()
        {
        }

        public Packet(byte[] dataStream)
        {
            //Data stream is the buffere where stuff is going to be stored.
            //with a MAX size limit
            int jsonsize = BitConverter.ToInt32(dataStream, 0);
            if (jsonsize > 0)
            {
                string stringjson = Encoding.UTF8.GetString(dataStream, 4, jsonsize);
                ReadData = JsonConvert.DeserializeObject<Dictionary<string, Object>>(stringjson);
            }
            else
            {
                ReadData.Clear();
            }
        }

        // Converts the packet into a byte array for sending/receiving 
        public byte[] GetDataStream()
        {
            string jsonoutput = jsonoutput = JsonConvert.SerializeObject(ReadData, Formatting.Indented);
            byte[] dataStream = Encoding.ASCII.GetBytes(jsonoutput);
            byte[] jsonlength = BitConverter.GetBytes(dataStream.Length);

            //join both arrays
            byte[] combined = new byte[dataStream.Length + jsonlength.Length];
            Array.Copy(dataStream, 0, combined, jsonlength.Length, dataStream.Length);
            Array.Copy(jsonlength, 0, combined, 0, jsonlength.Length);

            return combined;
        }

        public int GetInt(string Name)
        {
            return Convert.ToInt32(ReadData[Name]);
        }


        #endregion
    }
}

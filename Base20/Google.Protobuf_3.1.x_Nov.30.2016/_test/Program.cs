using Google.Protobuf;
using Google.Protobuf.Examples.AddressBook;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            // Serialize and parse it.  Make sure to parse from an InputStream, not
            // directly from a ByteString, so that CodedInputStream uses buffered
            // reading.
            AddressBook l1 = new AddressBook();
            l1.People.Add(new Person() { Id = 1, Name = "ABC", Email = "abc@gmail.com" });

            uint tagLengthDelimited = WireFormat.MakeTag(AddressBook.PeopleFieldNumber, WireFormat.WireType.LengthDelimited);


            //// Hand-craft the stream to contain a single entry with just a value.
            //using (var memoryStream = new MemoryStream())
            //using (var output = new CodedOutputStream(memoryStream))
            //{
            //    output.WriteTag(AddressBook.PeopleFieldNumber, WireFormat.WireType.LengthDelimited);

            //    // Size of the entry (tag, size written by WriteMessage, data written by WriteMessage)
            //    output.WriteLength(2 + l1.CalculateSize());
            //    output.WriteTag(2, WireFormat.WireType.LengthDelimited);
            //    output.WriteMessage(l1);
            //    output.Flush();

            //    byte[] bf = memoryStream.ToArray();
            //    var parsed = AddressBook.Parser.ParseFrom(bf);
            //}






            var clone = l1.Clone();
            byte[] bytes = l1.ToByteArray();
            string json1 = l1.ToString();
            string json2 = JsonFormatter.Default.Format(l1);
            var writer = new StringWriter();
            JsonFormatter.Default.Format(l1, writer);
            string json3 = writer.ToString();


            var empty = Empty.Parser.ParseFrom(bytes);

            // this works - json below
            JsonFormatter jsf = new JsonFormatter(new JsonFormatter.Settings(true));
            string jsonString = jsf.Format(l1);
            // this throws error - see exception details below
            var lsJson = JsonParser.Default.Parse<AddressBook>(jsonString);
            var lsJson2 = JsonParser.Default.Parse(jsonString, AddressBook.Descriptor);

            // MessageDescriptor -> 

            //string jsonMessageDescriptor = JsonConvert.SerializeObject(AddressBook.Descriptor);


            byte[] buf = null; 
            using (var ms = new MemoryStream())
            using (var output = new CodedOutputStream(ms))
            {
                //////uint tag = WireFormat.MakeTag(1, WireFormat.WireType.LengthDelimited);
                //////output.WriteRawVarint32(tag);
                //////output.WriteRawVarint32(0x7FFFFFFF);
                //////output.WriteRawBytes(new byte[32]); // Pad with a few random bytes.
                //////output.Flush();
                //////ms.Position = 0;

                ////output.WriteTag(1, WireFormat.WireType.Varint);
                //output.WriteInt32(99);
                ////output.WriteTag(2, WireFormat.WireType.Varint);
                //output.WriteInt32(123);

                //output.WriteFixed32(0);
                //output.WriteFixed32(34567);
                //output.WriteRawVarint32(0);
                //output.WriteRawVarint32(34567);
                //output.WriteLength(7);

                output.WriteRawVarint32(99);
                output.WriteFixed32(34567);

                l1.WriteTo(output);

                output.Flush();

                buf = ms.ToArray();
            }

            byte type = 0;
            uint tag = 0;
            AddressBook l2 = null;
            string log;
            if (buf != null && buf.Length > 0)
            {
                //using (var input = new CodedInputStream(buf))
                //{
                //    //tag = input.ReadInt32();
                //    tag = input.ReadFixed32();
                //    try
                //    {
                //        l2 = AddressBook.Parser.ParseFrom(input);
                //    }
                //    catch (Exception ex)
                //    {
                //        log = ex.Message;
                //    }
                //}

                type = buf[0];
                tag = BitConverter.ToUInt32(buf, 1);

                using (CodedInputStream input = new CodedInputStream(buf, 5, buf.Length - 5))
                {
                    try
                    {
                        l2 = AddressBook.Parser.ParseFrom(input);
                    }
                    catch (Exception ex)
                    {
                        log = ex.Message;
                    }
                }

            }



            ////////////////////////////////////////////////

            Console.WriteLine("Enter to exit ...");
            Console.ReadLine();
        }
    }
}

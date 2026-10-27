using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using VPay.Optum.Nc835Segments;

namespace VPay.Optum.Nc835FileProcessor.Application.Services
{
    public class X12Reader : IX12Reader
    {
        private readonly PipeReader _reader;

        public X12Reader(Stream stream)
        {
            _reader = PipeReader.Create(stream);
        }

        public async IAsyncEnumerable<object> GetNext()
        {
            X12File file = new();
            string code = "";

            X12Record? record = null;

            while (true)
            {
                ReadResult read = await _reader.ReadAsync();
                ReadOnlySequence<byte> buffer = read.Buffer;
                while (TryReadLine(ref buffer, out ReadOnlySequence<byte> sequence, file.ISA?.EndSegmentChar))
                {
                    var segment = ProcessSequence(sequence);
                    if (file.ISA == null)
                    {
                        file.ISA = new ISA(segment);
                        yield return file.ISA;
                    }
                    else
                    {
                        var arr = segment.Trim().Split(file.ISA.SegmentSeparator);

                        switch (arr[0])
                        {
                            case "GS":
                                file.GS = new GS(arr);
                                yield return file.GS;
                                break;

                            case "ST":
                                record = new X12Record()
                                {
                                    ST = new ST(arr)
                                };
                                break;

                            case "BPR":
                                record!.BPR = new BPR(arr);
                                break;

                            case "TRN":
                                record!.TRN = new TRN(arr);
                                break;

                            case "DTM":
                                record!.DTM = new DTM(arr);
                                break;

                            case "N1":
                                var n1 = new N1(arr);
                                code = n1.N101;

                                if (code == "PR")
                                {
                                    record!.Loop1000A.PayerIdentification = n1;
                                }
                                else if (code == "PE")
                                {
                                    record!.Loop1000B.PayeeIdentification = n1;
                                }
                                break;

                            case "N3":
                                var n3 = new N3(arr);

                                if (code == "PR")
                                {
                                    record!.Loop1000A.PayerAddress = n3;
                                }
                                else if (code == "PE")
                                {
                                    record!.Loop1000B.PayeeAddress = n3;
                                }
                                break;

                            case "N4":
                                var n4 = new N4(arr);

                                if (code == "PR")
                                {
                                    record!.Loop1000A.PayerCityStateZip = n4;
                                }
                                else if (code == "PE")
                                {
                                    record!.Loop1000B.PayeeCityStateZip = n4;
                                }
                                break;

                            case "SE":
                                record!.SE = new SE(arr);
                                yield return record;
                                record = null;
                                break;

                            case "GE":
                                file.GE = new GE(arr);
                                yield return file.GE;
                                break;

                            case "IEA":
                                file.IEA = new IEA(arr);
                                yield return file.IEA;
                                break;
                        }
                    }
                }

                _reader.AdvanceTo(buffer.Start, buffer.End);
                if (read.IsCompleted)
                {
                    break;
                }
            }
        }

        private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line, char? separator)
        {
            if (separator == null)
            {
                if (buffer.Length < 106)
                {
                    line = default;
                    return false;
                }
                var pos = buffer.GetPosition(106);
                line = buffer.Slice(0, pos);
                buffer = buffer.Slice(buffer.GetPosition(1, pos));
                return true;
            }

            var position = buffer.PositionOf((byte)separator);
            if (position == null)
            {
                line = default;
                return false;
            }

            line = buffer.Slice(0, position.Value);
            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

            return true;
        }

        private static string ProcessSequence(ReadOnlySequence<byte> sequence)
        {
            if (sequence.IsSingleSegment)
            {
                return Parse(sequence.FirstSpan);
            }

            Span<byte> span = stackalloc byte[(int)sequence.Length];
            sequence.CopyTo(span);

            return Parse(span);
        }

        private static string Parse(ReadOnlySpan<byte> bytes)
        {
            Span<char> chars = stackalloc char[bytes.Length];
            Encoding.UTF8.GetChars(bytes, chars);

            return chars.ToString();
        }
    }
}

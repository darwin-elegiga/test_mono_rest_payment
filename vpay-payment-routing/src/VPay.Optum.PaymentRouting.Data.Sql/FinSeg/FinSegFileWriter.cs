using System.IO;
using System.Threading.Tasks;
using FlatFiles;
using FlatFiles.TypeMapping;
using VPay.Optum.PaymentRouting.Data.Sql.Models;

namespace VPay.Optum.PaymentRouting.Data.Sql.FinSeg
{
    public class FinSegFileWriter : IFinSegFileWriter
    {
        internal FixedLengthTypeMapperInjector Injector { get; }

        public FinSegFileWriter()
        {
            Injector = GetInjector();
        }

        public async Task BuildFile(TextWriter writer, FinSegFile file)
        {
            var typedWriter = Injector.GetWriter(writer, new FixedLengthOptions { RecordSeparator = "\n" });

            await typedWriter.WriteAsync(file.Header).ConfigureAwait(false);

            foreach (var record in file.Details)
            {
                await typedWriter.WriteAsync(record).ConfigureAwait(false);
            }

            await typedWriter.WriteAsync(file.Trailer).ConfigureAwait(false);
        }

        private static FixedLengthTypeMapperInjector GetInjector()
        {
            var selector = new FixedLengthTypeMapperInjector();

            selector.WithDefault(GetFinSegDetailMapper());
            selector.When<FinSegFileHeader>().Use(GetHeaderMapper());
            selector.When<FinSegFileTrailer>().Use(GetTrailerMapper());

            return selector;
        }

        private static IFixedLengthTypeMapper<FinSegFileHeader> GetHeaderMapper()
        {
            var mapper = FixedLengthTypeMapper.Define<FinSegFileHeader>();

            mapper.CustomMapping(new StringColumn("RecordType"), 2)
                .WithWriter(_ => "10");

            mapper.CustomMapping(new StringColumn("FileDate"), 8)
                .WithWriter(d => d.FileDate.ToString("yyyyMMdd"));

            mapper.CustomMapping(new StringColumn("FileTime"), 6)
                .WithWriter(d => d.FileDate.ToString("HHmmss"));

            mapper.CustomMapping(new StringColumn("FileName"), 40)
                .WithWriter(d =>
                d.FileName?.IndexOf(".") <= 0 ? (d.FileName ?? string.Empty) :
                d.FileName.Substring(0, d.FileName.LastIndexOf("."))
                );

            return mapper;
        }

        private static IFixedLengthTypeMapper<FinSegFileTrailer> GetTrailerMapper()
        {
            var mapper = FixedLengthTypeMapper.Define<FinSegFileTrailer>();

            mapper.CustomMapping(new StringColumn("RecordType"), 2)
                .WithWriter(_ => "90");

            mapper.Property(d => d.DetailCount, new Window(9) { Alignment = FixedAlignment.RightAligned, FillCharacter = '0' });
            mapper.Property(d => d.TotalRecords, new Window(9) { Alignment = FixedAlignment.RightAligned, FillCharacter = '0' });

            return mapper;
        }

        private static IFixedLengthTypeMapper<FinSegFileDetail> GetFinSegDetailMapper()
        {
            var mapper = FixedLengthTypeMapper.Define<FinSegFileDetail>();

            mapper.CustomMapping(new StringColumn("RecordType"), 2)
                .WithWriter(_ => "20");

            mapper.Property(d => d.PayerId, 80);
            mapper.CustomMapping(new StringColumn("Amount"), new Window(12) { Alignment = FixedAlignment.RightAligned, FillCharacter = '0' })
                .WithWriter(d => d.Amount.ToString("0.00").Replace(".", ""));
            mapper.Property(d => d.ProcessDate, 8).OutputFormat("yyyMMdd");

            return mapper;
        }
    }
}

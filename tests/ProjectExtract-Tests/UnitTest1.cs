using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Models;
using Npgquery;
using Npgsql;
using PgQuery;
using System.Text;

namespace ProjectExtract_Tests
{
    public class Tests
    {

        private string connString = "Host=192.168.12.96;Port=5432;Database=lego;Username=postgres;Password=mysecretpassword";
        private Parser _parser;

        [SetUp]
        public void Setup()
        {
            var connString = "Host=192.168.12.96;Port=5432;Database=lego;Username=postgres;Password=mysecretpassword";
            _parser = new Parser();
        }

        [TearDown]
        public void TearDown()
        {
            _parser.Dispose();
        }

        [Test]
        public async Task Test1()
        {
            PgProjectExtractor extractor = new PgProjectExtractor(connString);
            var ver = await extractor.DetectPostgresVersion();
            var project = await extractor.ExtractPgProject("Lego.DB", ver);

            var fs = new FileStream("./Lego.DB.pgpac", FileMode.Create, FileAccess.Write);
            
            await PgProject.Save(project, fs);    
            fs.Close();
        }
        

        



        
    }
}
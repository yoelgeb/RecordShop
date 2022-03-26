using Npgsql;

namespace CSS475FinalProject.HelperClasses
{
    public static class Helper
    {
        public static string postgresConnectionString = "Server=css475.postgres.database.azure.com; User Id=luca; Database=recordstore; Port=5432; Password=FinalCss475;SSLMode=Prefer";

        public static NpgsqlConnection postgresConnection = new NpgsqlConnection(postgresConnectionString);

        public enum Search
        {
            AlbumsWithGivenSong,
            AlbumsByArtist,
            AlbumsInGivenGenre,
            AlbumsInGivenEra,
            AlbumsOfGivenType,
            RecordsOfGivenAlbum,
            RecordsComingIn,
            RecordsInStock,
            ArtistOnGivenSong,
            RecordWithBarcode
        }

        public enum Update
        {
            UpdateRecordPrice,
            UpdateRecordCondition,
            UpdateRecordStatus
        }

        public enum Status
        {
            IC,
            IS,
            OS,
            S
        }

        public enum Type
        {
            FortyFiveVinyl,
            ThirtyVinyl,
            CompactDisc,
            Cassette
        };

        public enum Condition
        {
            N,
            U
        }


        public static void InitializeHelper()
        {
            // Nothing for now!
            NpgsqlConnection.GlobalTypeMapper.UseJsonNet();
        }
    }
}

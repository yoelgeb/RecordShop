using Microsoft.AspNetCore.Mvc;
using Npgsql;
using CSS475FinalProject.HelperClasses;
using System.Transactions;

namespace CSS475FinalProject.Pages
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatabaseController : ControllerBase
    {
        [HttpPost]
        public string PostData([FromBody] SearchQuery searchQuery)
        {
            string query;

            switch (searchQuery.Value)
            {
                case (int)Helper.Search.AlbumsWithGivenSong:
                    query = string.Format("SELECT album.Title AS \"Album Title\", Album.maincon AS \"Artist Or Band\" FROM ALBUM JOIN SongList ON(Songlist.AlbumID = Album.id) " +
                        "JOIN Song ON(Song.id = Songlist.songid) WHERE song.Title ILIKE '{0}' ORDER BY Album.title DESC", searchQuery.Input);  
                    break;
                
                // RETURNS two columns: artistname and bandname // one column is always null
                // currently breaks when attempting to output a null
                case (int)Helper.Search.ArtistOnGivenSong:
                    query = string.Format("SELECT * FROM " +
                                        "(SELECT artist.name AS \"Artist Or Band\" FROM song JOIN artistcontributor ON (song.id = artistcontributor.songid) JOIN artist ON (artistcontributor.artistid = artist.id) WHERE song.title ILIKE '{0}') AS SongArtist " +
                                        "FULL JOIN (SELECT band.name AS \"Artist Or Band\" FROM song JOIN bandcontributor ON (song.id = bandcontributor.songid) JOIN band ON (bandcontributor.bandid = band.id) WHERE song.title ILIKE '{0}') AS SongBand ON (false) " +
                                        "ORDER BY SongArtist.\"Artist Or Band\" DESC, SongBand.\"Artist Or Band\" DESC", searchQuery.Input);
                    break;

                case (int)Helper.Search.AlbumsByArtist:
                    query = string.Format("SELECT AlbumBand.title AS \"Album Title\", AlbumMember.title AS \"Album Title\", AlbumArtist.title AS \"Album Title\" FROM ( " + 
                                        "SELECT DISTINCT Album.year, Album.title FROM Album JOIN SongList ON(album.id = songlist.albumid) JOIN Song ON(songlist.songid = song.id) JOIN BandContributor ON(BandContributor.songid = song.id) JOIN Band ON(BandContributor.bandid = band.id) WHERE Band.name ILIKE '{0}' ORDER BY album.year DESC) AS AlbumBand " +
                                        "FULL JOIN(SELECT DISTINCT Album.year, Album.title FROM Album JOIN SongList ON (album.id = songlist.albumid) JOIN Song ON(songlist.songid = song.id) JOIN BandContributor ON(BandContributor.songid = song.id) JOIN Band ON(BandContributor.bandid = band.id) JOIN BandMember ON(Band.id = BandMember.bandid) JOIN Artist ON(Bandmember.artistid = artist.id) WHERE Artist.name ILIKE '{0}' ORDER BY album.year DESC) AS AlbumMember ON(false) " +
                                        "FULL JOIN(SELECT DISTINCT Album.year, Album.title FROM Album JOIN SongList ON (album.id = songlist.albumid) JOIN Song ON(songlist.songid = song.id) JOIN ArtistContributor ON(song.id = artistcontributor.songid) JOIN Artist ON(artistcontributor.artistid = artist.id) WHERE Artist.name ILIKE '{0}' ORDER BY album.year DESC) AS AlbumArtist ON(false)", searchQuery.Input);
                    break;


                case (int)Helper.Search.AlbumsInGivenGenre:
                    query = string.Format("SELECT album.title AS \"Album Title\", album.maincon AS \"Artist Or Band\" FROM Album " +
                        "JOIN Genre ON (Genre.type = Album.genreid) WHERE Genre.description ILIKE '{0}' ORDER BY Album.maincon DESC, album.title DESC", searchQuery.Input);
                    break;

                case (int)Helper.Search.AlbumsInGivenEra:
                    query = string.Format("SELECT album.title AS \"Album Title\", album.maincon AS \"Artist Or Band\" FROM ALBUM " +
                        "WHERE Album.year::INTEGER >= {0} AND album.year::INTEGER < {0} + 10 ORDER BY Album.maincon DESC, Album.title DESC", searchQuery.Input);
                    break;

                case (int)Helper.Search.AlbumsOfGivenType:
                    query = string.Format("SELECT album.title AS \"Album Title\", Album.maincon AS \"Artist Or Band\" FROM Record " +
                        "JOIN Album ON(Album.id = Record.albumid) WHERE Record.recordtype ILIKE '{0}' ORDER BY Album.maincon DESC, Album.title DESC", searchQuery.Input);
                    break;

                case (int)Helper.Search.RecordsOfGivenAlbum:
                    query = string.Format("SELECT record.barcode AS \"Barcode\", album.maincon AS \"Artist Or Band\", Recordtype.name AS \"Record Type\", Record.statusid AS \"Status\", Record.condition AS \"Condition\", Record.price::VARCHAR AS \"Price\" FROM Record " +
                        "LEFT JOIN Album ON(Album.id = Record.albumid) JOIN RecordType ON(Recordtype.type = Record.recordtype) WHERE album.title ILIKE '{0}' " +
                        "ORDER BY Album.maincon DESC, Recordtype.name DESC", searchQuery.Input);
                    break;

                case (int)Helper.Search.RecordsComingIn:
                    query = string.Format("SELECT record.barcode AS \"Barcode\", album.title AS \"Album Title\", album.maincon AS \"Artist Or Band\", recordtype.name AS \"Record Type\" FROM Record JOIN Album ON(Album.id = Record.albumid) " +
                        "JOIN Status ON(Status.id = Record.statusid) " +
                        "JOIN RecordType ON(Recordtype.type = Record.recordtype) WHERE Status.Description ILIKE 'incoming'" +
                        " ORDER BY Album.maincon DESC, Album.title DESC");
                    break;

                case (int)Helper.Search.RecordsInStock:
                    query = string.Format("SELECT record.barcode AS \"Barcode\", album.title AS \"Album Title\", album.maincon AS \"Artist Or Band\", recordtype.name AS \"Record Type\", Record.price::VARCHAR AS \"Price\" FROM Record JOIN Album ON(Album.id = Record.albumid) " +
                        "JOIN Status ON(Status.id = Record.statusid) " +
                        "JOIN RecordType ON(Recordtype.type = Record.recordtype) WHERE Status.Description ILIKE 'In Stock'" +
                        "ORDER BY album.maincon DESC, album.Title DESC");
                    break;

                case (int)Helper.Search.RecordWithBarcode:
                    query = string.Format("SELECT record.barcode AS \"Barcode\", album.maincon AS \"Artist Or Band\", Recordtype.name AS \"Record Type\", Record.statusid AS \"Status\",Record.condition AS \"Condition\", Record.price::VARCHAR AS \"Price\" FROM Record " +
                        "LEFT JOIN Album ON(Album.id = Record.albumid) JOIN RecordType ON(Recordtype.type = Record.recordtype) WHERE record.barcode ILIKE '{0}' " +
                        "ORDER BY Album.maincon DESC, Recordtype.name DESC", searchQuery.Input);
                    break;

                default:
                    return "something very bad has happened!";
            }

            return TableToJson(query);
        }

        [HttpPut]
        public string InsertRecord([FromBody] InsertInfo insertInfo)
        {
            // Cannot have numbers in enum
            string type;

            string price = insertInfo.Price;
            if (price != null && !price.Contains('.'))
                price += ".00";

            switch (insertInfo.Type)
            {
                case (int)Helper.Type.FortyFiveVinyl:
                    type = "45";
                    break;

                case (int)Helper.Type.ThirtyVinyl:
                    type = "30";
                    break;

                case (int)Helper.Type.CompactDisc:
                    type = "CD";
                    break;
                
                case (int)Helper.Type.Cassette:
                    type = "CS";
                    break;

                default:
                    type = "something very bad has happened";
                    break;
            }

            string checkEmployee = string.Format("SELECT EXISTS (SELECT id FROM Employee WHERE employeenumber = '{0}' AND password = '{1}')",
                        insertInfo.EmployeeNumber, insertInfo.EmployeePassword);

            string insert = string.Format("INSERT INTO Record(Barcode, AlbumID, recordType, Condition, Price, StatusID) " +
                                          "VALUES ('{0}', (SELECT id FROM Album WHERE Album.maincon ILIKE '{1}' AND Album.title ILIKE '{2}'), '{3}', '{4}', '{5}'::decimal, '{6}')", 
                                          insertInfo.Barcode, //0
                                          insertInfo.AlbumArtist, //1
                                          insertInfo.AlbumTitle, //2
                                          type, //3
                                          ((Helper.Condition)insertInfo.Condition).ToString(), //4
                                          price, //5
                                          ((Helper.Status)insertInfo.Status).ToString() //6
                                          );

            return InsertAndReturnResult(insert, checkEmployee);
        }

        [HttpPatch]
        public string UpdateRecord([FromBody] UpdateInfo updateInfo)
        {
            string insert;

            string checkEmployee = string.Format("SELECT EXISTS (SELECT id FROM Employee WHERE employeenumber = '{0}' AND password = '{1}')",
                                    updateInfo.EmployeeNumber, updateInfo.EmployeePassword);

            switch (updateInfo.Value)
            {
                case (int)Helper.Update.UpdateRecordPrice:
                    insert = string.Format("UPDATE record " +
                            "SET price = '{0}'::money " +
                            "WHERE barcode = '{1}' ", updateInfo.Input, updateInfo.Barcode);

                    break;
                case (int)Helper.Update.UpdateRecordCondition:
                    insert = string.Format("UPDATE record " +
                            "SET condition = '{0}' " +
                            "WHERE barcode = '{1}'", updateInfo.Input, updateInfo.Barcode);

                    break;
                case (int)Helper.Update.UpdateRecordStatus:
                    insert = string.Format("UPDATE record " +
                            "SET statusid = '{0}' " +
                            "WHERE barcode = '{1}'", updateInfo.Input, updateInfo.Barcode);

                    break;
                default:
                    return "Something bad has happened!";
            }

            return InsertAndReturnResult(insert, checkEmployee);
        }

        [HttpDelete]
        public string SellRecord([FromBody] SellInfo sellInfo)
        {
            string now = DateTime.Now.ToString();

            string checkEmployee = string.Format("SELECT EXISTS (SELECT id FROM Employee WHERE employeenumber = '{0}' AND password = '{1}')",
                                                sellInfo.EmployeeNumber, sellInfo.EmployeePassword);

            string sellRecord = string.Format("UPDATE record " +
                                              "SET statusid = 'S' " +
                                              "WHERE barcode = '{0}'", sellInfo.Barcode);

            string addTransaction = string.Format("INSERT INTO Transaction (PurchaseTime, Employee) " +
                                                  "VALUES ('{0}', (SELECT id FROM Employee WHERE employeenumber = '{1}')) ", now, sellInfo.EmployeeNumber);

            string connectRecord = string.Format("UPDATE record " +
                                                 "SET TransactionID = (SELECT id FROM Transaction ORDER BY id DESC LIMIT 1) " +
                                                 "WHERE Barcode = '{0}'", sellInfo.Barcode);

            try
            {
                // Create the TransactionScope to execute the commands, guaranteeing
                // that both commands can commit or roll back as a single unit of work.
                // Very sloppy copy pasted code
                using (TransactionScope scope = new TransactionScope())
                {
                    var check = EmployeeChecker(checkEmployee);
                    Helper.postgresConnection.Close();

                    Helper.postgresConnection.Open();

                    if (check)
                    {
                        using (var command = new NpgsqlCommand(sellRecord, Helper.postgresConnection))
                        {
                            command.ExecuteNonQuery();
                        }

                        using (var command = new NpgsqlCommand(addTransaction, Helper.postgresConnection))
                        {
                            command.ExecuteNonQuery();
                        }

                        using (var command = new NpgsqlCommand(connectRecord, Helper.postgresConnection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        return "[{\"Sucess\":\"false\"}]";
                    }  
                    // The Complete method commits the transaction. If an exception has been thrown,
                    // Complete is not  called and the transaction is rolled back.
                    scope.Complete();
                    Helper.postgresConnection.Close();
                }
            }
            catch (TransactionAbortedException ex)
            {
                Helper.postgresConnection.Close();

                return "[{\"Sucess\":\"false\"}]";
            }

            return "[{\"Sucess\":\"true\"}]";
        }

        public bool EmployeeChecker(String query)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                Helper.postgresConnection.Open();
                using (var command = new NpgsqlCommand(query, Helper.postgresConnection))
                {
                    var reader = command.ExecuteReader();
                    var total = reader.FieldCount;
                    while (reader.Read())
                    {
                        scope.Complete();
                        return reader.GetBoolean(0);
                    }
                }
            }
            return false;
        }

        public string InsertAndReturnResult(string insert, string employeeCheck)
        {
            using (TransactionScope scope = new TransactionScope())
            {   
                var check = EmployeeChecker(employeeCheck);
                Helper.postgresConnection.Close();

                Helper.postgresConnection.Open();
                if (check)
                {
                    using (var command = new NpgsqlCommand(insert, Helper.postgresConnection))
                    {
                        try
                        {
                            if (command.ExecuteNonQuery() > 0)
                            {
                                scope.Complete();
                                Helper.postgresConnection.Close();
                                return "[{\"Sucess\":\"true\"}]";
                            }
                        }
                        catch (Exception ex)
                        {
                            Helper.postgresConnection.Close();
                            return "[{\"Sucess\":\"false\"}]";
                        }
                    }
                }

                Helper.postgresConnection.Close();
                return "[{\"Sucess\":\"false\"}]";
            }
        }

        public string TableToJson(string query)
        {
            Helper.postgresConnection.Open();

            var output = "[";

            using (var command = new NpgsqlCommand(query, Helper.postgresConnection))
            {
                var reader = command.ExecuteReader();
                var total = reader.FieldCount;
                while (reader.Read())
                {
                    output += "{";

                    for (int i = 0; i < total; i++)
                    {
                        if (!reader.IsDBNull(i))
                            output += String.Format("\"{0}\":\"{1}\",", reader.GetName(i), reader.GetString(i));
                    }

                    // output = output.Substring(0, output.Length - 2);

                    output += "},";
                }

                Helper.postgresConnection.Close();
            }

            output = output[0..^1];

            output += "]";

            return output;
        }
    }

    public class SearchQuery
    {
        public int Value { get; set; }

        public string? Input { get; set; }
    }

    public class UpdateInfo
    {
        public string? EmployeeNumber { get; set; }

        public string? EmployeePassword { get; set; }

        public string? Barcode { get; set; }

        public int Value { get; set; }

        public string? Input { set; get; }
    }

    public class SellInfo
    {
        public string? EmployeeNumber { get; set; }

        public string? EmployeePassword { get; set; }

        public string? Barcode { get; set; }
    }

    public class InsertInfo
    {
        public string? EmployeeNumber { get; set; }

        public string? EmployeePassword { get; set; }

        public string? Barcode { get; set; }

        public string? AlbumTitle { get; set; }

        public string? AlbumArtist { get; set; }

        public string? Price { get; set; }

        public int Status { set; get; }

        public int Type { set; get; }

        public int Condition { set; get; }
    }
}

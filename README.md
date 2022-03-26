# CSS475FinalProject

API:
Use visual studio 2022. Not sure if nuget packages will come with the repo but just search for npsql for postgres' package

Right click on CSS475FinalProject not Solution 'CSS475FinalProject' (1 of 1 project) and click [publish...] make sure that you select the .yml publishing scheme so that
whenever you push you also push the head to the cloud

Database:
Using psql go where normally you would log into your psql database and copy:
psql "host=css475.postgres.database.azure.com port=5432 dbname=postgres user=luca password=FinalCss475 sslmode=require"

now you have command line access to the database!

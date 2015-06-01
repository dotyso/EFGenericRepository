using CCWOnline.Management.EntityFramework;
using CCWOnline.Management.Models;
using System;
using System.Collections.Generic;

namespace ConsoleApplication2
{
    class Program
    {
        static void Main(string[] args)
        {
            //Count
            ConferenceRepository conferenceRepository = new ConferenceRepository();
            int count = conferenceRepository.Count();
            Console.WriteLine("Count: {0}", count);

            //FindAll
            List<Conference> conferences = conferenceRepository.FindAll(p => p.ParticipantsNum < 100);
            Console.WriteLine("FindAll: {0}", conferences.Count);

            //SqlQuery
            List<Conference> conferences2 = conferenceRepository.SqlQuery("SELECT * FROM Conference WHERE ParticipantsNum < 100");
            Console.WriteLine("SqlQuery: {0}", conferences2.Count);

            //FindOne
            Conference conference = conferenceRepository.FindOne(p => p.ConferenceId == 48);
            Console.WriteLine("FindOne: {0}", conference.ParticipantsNum);

            //Update
            conference.ParticipantsNum++;
            conferenceRepository.Update(conference);
            Conference conference2 = conferenceRepository.FindOne(p => p.ConferenceId == 48);
            Console.WriteLine("FindOne: {0}", conference2.ParticipantsNum);

            //Delete
            conferenceRepository.Delete(p => p.ConferenceId == 47);
            Console.WriteLine("Count: {0}", conferenceRepository.Count());

            //FindAll Query And
            Query<Conference> query = new Query<Conference>();
            query.Where(p => p.ConferenceId < 150 || p.Name.Contains("是"));
            query.WhereAnd(p => p.Status == 2);
            query.Limit = 3;
            List<Conference> conferences3 = conferenceRepository.FindAll(query);
            Console.WriteLine("FindAll Query: {0}", conferences3.Count);


            //FindAll OrderBy ThenBy
            Query<Conference> query2 = new Query<Conference>(p => p.ParticipantsNum > 1);
            query2.OrderBy(p => p.Status).ThenByDescending(p => p.ConferenceId);
            List<Conference> conferences4 = conferenceRepository.FindAll(query2);
            Console.WriteLine("FindAll OrderBy: {0}", conferences4.Count);

            //FindAll Paging
            int totalCount = 0;
            Query<Conference> query3 = new Query<Conference>(p => p.ParticipantsNum > 1);
            query3.OrderBy(p => p.Status).ThenByDescending(p => p.ConferenceId);
            List<Conference> conferences5 = conferenceRepository.FindAll(query3, 10, 20, out totalCount);
            Console.WriteLine("FindAll Paging: {0}", conferences5.Count);

            //Find All DynamicQuery
            Query<Conference> query4 = new Query<Conference>("ConferenceId < 100");
            query4.WhereAnd("Status = {0}", 1);
            query4.OrderBy("ConferenceId DESC");
            List<Conference> conferences6 = conferenceRepository.FindAll(query4);
            Console.WriteLine("FindAll DynamicQuery: {0}", conferences6.Count);

            Console.Read();
        }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SystemicalConsensusBot
{
    public class DatabaseConnection : IDatabaseConnection
    {
        private readonly string FilePath;
        private static readonly Random random = new Random();
        public DatabaseConnection(string databaseFilePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(databaseFilePath));
            if (!File.Exists(databaseFilePath)) File.WriteAllText(databaseFilePath, JsonConvert.SerializeObject(new Dictionary<long, Poll>()));
            FilePath = databaseFilePath;
        }

        private Dictionary<long, Poll> GetDict()
        {
            return JsonConvert.DeserializeObject<Dictionary<long, Poll>>(File.ReadAllText(FilePath));
        }

        private void SetDict(Dictionary<long, Poll> dict)
        {
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(dict));
        }

        public Poll GetPoll(long id)
        {
            var dict = GetDict();
            return dict[id];
        }

        public List<Poll> GetPollsByOwner(int ownerID)
        {
            var dict = GetDict();
            return dict.Where(x => x.Value.OwnerId == ownerID).Select(x => x.Value).ToList();
        }

        public Poll SavePoll(Poll poll)
        {
            lock (FilePath)
            {
                var dict = GetDict();
                if (poll.PollId == -1)
                {
                    long longRand;
                    do
                    {
                        byte[] buf = new byte[8];
                        random.NextBytes(buf);
                        longRand = BitConverter.ToInt64(buf, 0);
                    } while (longRand < 0 || dict.ContainsKey(longRand));
                    poll.PollId = longRand;
                }
                dict[poll.PollId] = poll;
                SetDict(dict);
            }
            return poll;
        }

        public void DeletePoll(long pollId) {
            lock (FilePath)
            {
                var dict = GetDict();
                dict.Remove(pollId);
                SetDict(dict);
            }
            
        }
    }
}

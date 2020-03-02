using System;
using System.Collections.Generic;
using System.Linq;
using TrainingSessionManagement.Helper;
using TrainingSessionManagement.Model;

namespace TrainingSessionManagement.Business
{
    public class SessionManagerBusiness : ISessionManagerBusiness
    {
        private const string _inputFile = "session_info.json";
        private Dictionary<int, int> _durationCountMap;
        private int minDuration;
        private List<int> _minRemainingDurationSnapshot;
        private IFileReader _fileReader;

        public SessionManagerBusiness(IFileReader fileReader)
        {
            _durationCountMap = new Dictionary<int, int>();
            _minRemainingDurationSnapshot = new List<int>();
            _fileReader = fileReader;
        }

        public List<TrackViewModel> GetTracks()
        {
            var tracks = new List<TrackViewModel>();
            try
            {
                // Read input data from json file
                var fileContents = _fileReader.GetFileContent(_inputFile);
                //var sessionInput = JsonConvert.DeserializeObject<List<SessionInputViewModel>>(fileContents);
                var sessionInput = _fileReader.GetInput(_inputFile);
                for (int i = 0; i < sessionInput.Count; i++)
                {
                    var session = sessionInput[i];

                    // Set duration as minutes
                    var minIndex = session.SessionDuration.IndexOf("min");
                    var minuteStr = minIndex >= 0 ? session.SessionDuration.Substring(0, minIndex) : (session.SessionDuration.Equals("lightning") ? "5" : string.Empty);
                    int.TryParse(minuteStr, out int minutes);
                    session.DurationMinutes = minutes;
                    session.IsAllocated = false;
                }

                SetDurationCountMap(sessionInput);
                var inValidSessions = sessionInput.Where(x => x.DurationMinutes > 240).ToList();
                var validSessions = sessionInput.Where(x => x.DurationMinutes <= 240).ToList();
                // continue untill all sessions are allocated to a slot
                while (validSessions.Any(x => !x.IsAllocated))
                {
                    var track = new TrackViewModel();
                    var hasMorningSessions = validSessions.Any(x => !x.IsAllocated && x.DurationMinutes <= 180);
                    if (hasMorningSessions)
                    {
                        var morningSessions = GetSessions(validSessions, SessionTypeEnum.Morning);
                        track.Sessions.AddRange(morningSessions);
                    }

                    var hasAfternoonSessions = validSessions.Any(x => !x.IsAllocated && x.DurationMinutes <= 240);
                    if (hasAfternoonSessions)
                    {
                        var afternoonSessions = GetSessions(validSessions, SessionTypeEnum.Afternoon);
                        track.Sessions.AddRange(afternoonSessions);
                    }

                    tracks.Add(track);
                }
                if (inValidSessions.Count > 0)
                {
                    inValidSessions.ForEach(x =>
                        tracks.Add(new TrackViewModel
                        {
                            Sessions = new List<Session>
                                {
                                    new Session
                                    {
                                        SessionName = x.SessionName,
                                        Duration = x.SessionDuration
                                    }
                                }
                        }
                    ));
                }
                for (int i = 0; i < tracks.Count; i++)
                {
                    tracks[i].TrackName = $"Track {i + 1}";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return tracks;
        }

        /// <summary>
        /// Set the _durationCountMap
        /// as a dictionary of all unique durations and their count
        /// </summary>
        /// <param name="sessionInput"></param>
        private void SetDurationCountMap(List<SessionInputViewModel> sessionInput)
        {
            foreach (var session in sessionInput)
            {
                if (_durationCountMap.ContainsKey(session.DurationMinutes))
                {
                    _durationCountMap[session.DurationMinutes]++;
                }
                else
                {
                    _durationCountMap.Add(session.DurationMinutes, 1);
                }
            }
        }

        private List<Session> GetSessions(List<SessionInputViewModel> sessionInput, SessionTypeEnum sessionType)
        {
            var result = new List<Session>();
            var usedSessionStack = new Stack<int>();
            var sessionsExplored = new Dictionary<int, HashSet<int>>();
            var uniqueDurationAvailable = _durationCountMap.Where(x => x.Value > 0).Select(x => x.Key).ToList();

            // If no unique duration left (i.e. all sessions already assigned a slot), then return empty list
            if (uniqueDurationAvailable.Count == 0)
            {
                return result;
            }

            minDuration = sessionType == SessionTypeEnum.Morning ? 180 : 240;
            var startTime = sessionType == SessionTypeEnum.Morning ? new DateTime(2020, 1, 1, 9, 0, 0) : new DateTime(2020, 1, 1, 13, 0, 0);

            var remainingDuration = ComputeRemainingDuration(minDuration, minDuration, usedSessionStack, sessionsExplored, uniqueDurationAvailable);

            if (remainingDuration == 0)
            {
                // Get all session duration from stack
                while (usedSessionStack.Count > 0)
                {
                    var sessionDuration = usedSessionStack.Pop();
                    var session = sessionInput.FirstOrDefault(x => x.DurationMinutes == sessionDuration && !x.IsAllocated);
                    if (session != null)
                    {
                        session.IsAllocated = true;
                        // Calculate next slot adding minutes
                        result.Add(new Session
                        {
                            Duration = session.SessionDuration,
                            SessionName = session.SessionName,
                            StartTime = startTime.Hour,
                            StartTimeText = startTime.ToString("hh:mm tt")
                        });
                        startTime = startTime.AddMinutes(session.DurationMinutes);
                    }
                }
            }
            else
            {
                // Adjust extra time here
                foreach (var item in _minRemainingDurationSnapshot)
                {
                    var session = sessionInput.FirstOrDefault(x => x.DurationMinutes == item && !x.IsAllocated);
                    if (session != null)
                    {
                        session.IsAllocated = true;
                        // Calculate next slot adding minutes
                        result.Add(new Session
                        {
                            Duration = session.SessionDuration,
                            SessionName = session.SessionName,
                            StartTime = startTime.Hour,
                            StartTimeText = startTime.ToString("hh:mm tt")
                        });
                        startTime = startTime.AddMinutes(session.DurationMinutes);
                    }
                }
            }

            if (sessionType == SessionTypeEnum.Afternoon && result.Count > 0)
            {
                if (startTime.Hour < 16)
                {
                    startTime = new DateTime(2020, 1, 1, 16, 0, 0);
                }
                result.Add(new Session
                {
                    SessionName = "Sharing Session",
                    StartTimeText = startTime.ToString("hh:mm tt"),
                    Duration = string.Empty
                });
            }
            else if (sessionType == SessionTypeEnum.Morning)
            {
                result.Add(new Session
                {
                    SessionName = "Lunch",
                    StartTimeText = "12:00 PM",
                    Duration = string.Empty
                });
            }

            return result;
        }

        private int ComputeRemainingDuration(int totalDuration, int remainingDuration, Stack<int> usedSessionStack, Dictionary<int, HashSet<int>> sessionsExplored, List<int> uniqueDurationAvailable)
        {
            while (remainingDuration > 0)
            {
                if (_durationCountMap.ContainsKey(remainingDuration) && _durationCountMap[remainingDuration] > 0)
                {
                    _durationCountMap[remainingDuration]--;
                    usedSessionStack.Push(remainingDuration);
                    remainingDuration = 0;
                }
                else
                {
                    if (sessionsExplored.ContainsKey(totalDuration) && uniqueDurationAvailable.All(x => sessionsExplored[totalDuration].Contains(x)))
                    {
                        break;
                    }

                    int? currentDuration;
                    var traversedSet = new HashSet<int>();
                    sessionsExplored.TryGetValue(remainingDuration, out traversedSet);

                    var isRandomRequired = _durationCountMap.Any(x => x.Value > 0 && x.Key < remainingDuration && (traversedSet == null || !traversedSet.Contains(x.Key)));
                    if (isRandomRequired)
                    {
                        var sessionList = _durationCountMap.Where(x => x.Value > 0 && x.Key < remainingDuration).Select(x => x.Key).ToList();
                        if (traversedSet != null)
                        {
                            sessionList = sessionList.Except(traversedSet).ToList();
                        }

                        currentDuration = sessionList.RandomElement();

                        // check if remainingDuration not present in sessionsExplored
                        // or remainingDuration is present in sessionsExplored but value does not contain currentDuration
                        if (!sessionsExplored.ContainsKey(remainingDuration) ||
                            (sessionsExplored.ContainsKey(remainingDuration) && !sessionsExplored[remainingDuration].Contains(currentDuration.Value)))
                        {
                            usedSessionStack.Push(currentDuration.Value);
                            _durationCountMap[currentDuration.Value]--;
                            remainingDuration -= currentDuration.Value;
                            remainingDuration = ComputeRemainingDuration(totalDuration, remainingDuration, usedSessionStack, sessionsExplored, uniqueDurationAvailable);
                        }
                    }
                    else
                    {
                        if (remainingDuration < minDuration)
                        {
                            minDuration = remainingDuration;
                            _minRemainingDurationSnapshot.Clear();
                            usedSessionStack.ToList().ForEach(x => _minRemainingDurationSnapshot.Add(x));
                        }
                        // pop from stack
                        var sessionPopped = usedSessionStack.Pop();

                        // add to remaining duration
                        remainingDuration += sessionPopped;

                        // add to durationCountMap
                        _durationCountMap[sessionPopped]++;

                        // add / update unsuccessful paths to prevent further searching
                        if (sessionsExplored.ContainsKey(remainingDuration))
                        {
                            sessionsExplored[remainingDuration].Add(sessionPopped);
                        }
                        else
                        {
                            sessionsExplored.Add(remainingDuration, new HashSet<int> { sessionPopped });
                        }
                    }
                }
            }

            return remainingDuration;
        }

        //private List<Session> GetMorningSession(List<SessionInputViewModel> sessionInput)
        //{
        //    var remainingDuration = 180;
        //    var result = new List<Session>();
        //    var usedSessionStack = new Stack<int>();
        //    var sessionsExplored = new Dictionary<int, HashSet<int>>();
        //    minDuration = 180;
        //    var uniqueDurationAvailable = _durationCountMap.Where(x => x.Value > 0).Select(x => x.Key).ToList();
        //    remainingDuration = ValidateMorningSession(remainingDuration, usedSessionStack, sessionsExplored, uniqueDurationAvailable);

        //    var startTime = new DateTime(2020, 1, 1, 9, 0, 0);
        //    if (remainingDuration == 0)
        //    {
        //        // Get all session duration from stack
        //        while (usedSessionStack.Count > 0)
        //        {
        //            var sessionDuration = usedSessionStack.Pop();
        //            var session = sessionInput.FirstOrDefault(x => x.DurationMinutes == sessionDuration && !x.IsAllocated);
        //            if (session != null)
        //            {
        //                session.IsAllocated = true;
        //                // Calculate next slot adding minutes
        //                result.Add(new Session
        //                {
        //                    Duration = session.SessionDuration,
        //                    SessionName = session.SessionName,
        //                    StartTime = startTime.Hour,
        //                    StartTimeText = startTime.ToString("hh:mm tt")
        //                });
        //                startTime = startTime.AddMinutes(session.DurationMinutes);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        // Adjust extra time here
        //        foreach (var item in _minRemainingDurationSnapshot)
        //        {
        //            var session = sessionInput.FirstOrDefault(x => x.DurationMinutes == item && !x.IsAllocated);
        //            session.IsAllocated = true;
        //            // Calculate next slot adding minutes
        //            result.Add(new Session
        //            {
        //                Duration = session.SessionDuration,
        //                SessionName = session.SessionName,
        //                StartTime = startTime.Hour,
        //                StartTimeText = startTime.ToString("hh:mm tt")
        //            });
        //            startTime = startTime.AddMinutes(session.DurationMinutes);
        //        }
        //    }

        //    result.Add(new Session
        //    {
        //        SessionName = "Lunch",
        //        StartTimeText = startTime.ToString("hh:mm tt"),
        //        Duration = string.Empty
        //    });
        //    return result;
        //}

        //private int ValidateMorningSession(int remainingDuration, Stack<int> usedSessionStack, Dictionary<int, HashSet<int>> sessionsExplored, List<int> uniqueDurationAvailable)
        //{
        //    while (remainingDuration > 0)
        //    {
        //        if (_durationCountMap.ContainsKey(remainingDuration) && _durationCountMap[remainingDuration] > 0)
        //        {
        //            _durationCountMap[remainingDuration]--;
        //            usedSessionStack.Push(remainingDuration);
        //            remainingDuration = 0;
        //        }
        //        else
        //        {
        //            if (sessionsExplored.ContainsKey(180) && uniqueDurationAvailable.All(x => sessionsExplored[180].Contains(x)))
        //            {
        //                break;
        //            }

        //            int? currentDuration;
        //            var traversedSet = new HashSet<int>();
        //            sessionsExplored.TryGetValue(remainingDuration, out traversedSet);

        //            var isRandomRequired = _durationCountMap.Any(x => x.Value > 0 && x.Key < remainingDuration && (traversedSet == null || !traversedSet.Contains(x.Key)));
        //            if (isRandomRequired)
        //            {
        //                var sessionList = _durationCountMap.Where(x => x.Value > 0 && x.Key < remainingDuration).Select(x => x.Key).ToList();
        //                if (traversedSet != null)
        //                {
        //                    sessionList = sessionList.Except(traversedSet).ToList();
        //                }

        //                currentDuration = sessionList.RandomElement();

        //                // check if remainingDuration not present in sessionsExplored
        //                // or remainingDuration is present in sessionsExplored but value does not contain currentDuration
        //                if (!sessionsExplored.ContainsKey(remainingDuration) ||
        //                    (sessionsExplored.ContainsKey(remainingDuration) && !sessionsExplored[remainingDuration].Contains(currentDuration.Value)))
        //                {
        //                    usedSessionStack.Push(currentDuration.Value);
        //                    _durationCountMap[currentDuration.Value]--;
        //                    remainingDuration -= currentDuration.Value;
        //                    remainingDuration = ValidateMorningSession(remainingDuration, usedSessionStack, sessionsExplored, uniqueDurationAvailable);
        //                }
        //            }
        //            else
        //            {
        //                if (remainingDuration < minDuration)
        //                {
        //                    minDuration = remainingDuration;
        //                    _minRemainingDurationSnapshot.Clear();
        //                    usedSessionStack.ToList().ForEach(x => _minRemainingDurationSnapshot.Add(x));
        //                }
        //                // pop from stack
        //                var sessionPopped = usedSessionStack.Pop();

        //                // add to remaining duration
        //                remainingDuration += sessionPopped;

        //                // add to durationCountMap
        //                _durationCountMap[sessionPopped]++;

        //                // add / update unsuccessful paths to prevent further searching
        //                if (sessionsExplored.ContainsKey(remainingDuration))
        //                {
        //                    sessionsExplored[remainingDuration].Add(sessionPopped);
        //                }
        //                else
        //                {
        //                    sessionsExplored.Add(remainingDuration, new HashSet<int> { sessionPopped });
        //                }
        //            }
        //        }
        //    }

        //    return remainingDuration;
        //}

        //private List<Session> GetAfternoonSession(List<SessionInputViewModel> sessionInput)
        //{
        //    var remainingDuration = 240;
        //    var result = new List<Session>();
        //    var usedSessionStack = new Stack<int>();
        //    var sessionsExplored = new Dictionary<int, HashSet<int>>();
        //    minDuration = 240;
        //    var uniqueDurationAvailable = _durationCountMap.Where(x => x.Value > 0).Select(x => x.Key).ToList();
        //    remainingDuration = ValidateAfternoonSession(remainingDuration, usedSessionStack, sessionsExplored, uniqueDurationAvailable);

        //    var startTime = new DateTime(2020, 1, 1, 13, 0, 0);
        //    if (remainingDuration == 0)
        //    {
        //        // Get all session duration from stack
        //        while (usedSessionStack.Count > 0)
        //        {
        //            var sessionDuration = usedSessionStack.Pop();
        //            var session = sessionInput.FirstOrDefault(x => x.DurationMinutes == sessionDuration && !x.IsAllocated);
        //            if (session != null)
        //            {
        //                session.IsAllocated = true;
        //                // Calculate next slot adding minutes
        //                result.Add(new Session
        //                {
        //                    Duration = session.SessionDuration,
        //                    SessionName = session.SessionName,
        //                    StartTime = startTime.Hour,
        //                    StartTimeText = startTime.ToString("hh:mm tt")
        //                });
        //                startTime = startTime.AddMinutes(session.DurationMinutes);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        // Adjust extra time here
        //        foreach (var item in _minRemainingDurationSnapshot)
        //        {
        //            var session = sessionInput.FirstOrDefault(x => x.DurationMinutes == item && !x.IsAllocated);
        //            session.IsAllocated = true;
        //            // Calculate next slot adding minutes
        //            result.Add(new Session
        //            {
        //                Duration = session.SessionDuration,
        //                SessionName = session.SessionName,
        //                StartTime = startTime.Hour,
        //                StartTimeText = startTime.ToString("hh:mm tt")
        //            });
        //            startTime = startTime.AddMinutes(session.DurationMinutes);
        //        }
        //    }

        //    result.Add(new Session
        //    {
        //        SessionName = "Sharing Session",
        //        StartTimeText = startTime.ToString("hh:mm tt"),
        //        Duration = string.Empty
        //    });

        //    return result;
        //}

        //private int ValidateAfternoonSession(int remainingDuration, Stack<int> usedSessionStack, Dictionary<int, HashSet<int>> sessionsExplored, List<int> uniqueDurationAvailable)
        //{
        //    while (remainingDuration > 0)
        //    {
        //        if (_durationCountMap.ContainsKey(remainingDuration) && _durationCountMap[remainingDuration] > 0)
        //        {
        //            _durationCountMap[remainingDuration]--;
        //            usedSessionStack.Push(remainingDuration);
        //            remainingDuration = 0;
        //        }
        //        else
        //        {
        //            if (sessionsExplored.ContainsKey(240) && uniqueDurationAvailable.All(x => sessionsExplored[240].Contains(x)))
        //            {
        //                break;
        //            }

        //            int? currentDuration;
        //            var traversedSet = new HashSet<int>();
        //            sessionsExplored.TryGetValue(remainingDuration, out traversedSet);

        //            var isRandomRequired = _durationCountMap.Any(x => x.Value > 0 && x.Key < remainingDuration && (traversedSet == null || !traversedSet.Contains(x.Key)));
        //            if (isRandomRequired)
        //            {
        //                var sessionList = _durationCountMap.Where(x => x.Value > 0 && x.Key < remainingDuration).Select(x => x.Key).ToList();
        //                if (traversedSet != null)
        //                {
        //                    sessionList = sessionList.Except(traversedSet).ToList();
        //                }

        //                currentDuration = sessionList.RandomElement();

        //                // check if remainingDuration not present in sessionsExplored
        //                // or remainingDuration is present in sessionsExplored but value does not contain currentDuration
        //                if (!sessionsExplored.ContainsKey(remainingDuration) ||
        //                    (sessionsExplored.ContainsKey(remainingDuration) && !sessionsExplored[remainingDuration].Contains(currentDuration.Value)))
        //                {
        //                    usedSessionStack.Push(currentDuration.Value);
        //                    _durationCountMap[currentDuration.Value]--;
        //                    remainingDuration -= currentDuration.Value;
        //                    remainingDuration = ValidateAfternoonSession(remainingDuration, usedSessionStack, sessionsExplored, uniqueDurationAvailable);
        //                }
        //            }
        //            else
        //            {
        //                if (remainingDuration < minDuration)
        //                {
        //                    minDuration = remainingDuration;
        //                    _minRemainingDurationSnapshot.Clear();
        //                    usedSessionStack.ToList().ForEach(x => _minRemainingDurationSnapshot.Add(x));

        //                }
        //                // pop from stack
        //                var sessionPopped = usedSessionStack.Pop();

        //                // add to remaining duration
        //                remainingDuration += sessionPopped;

        //                // add to durationCountMap
        //                _durationCountMap[sessionPopped]++;

        //                // add / update unsuccessful paths to prevent further searching
        //                if (sessionsExplored.ContainsKey(remainingDuration))
        //                {
        //                    sessionsExplored[remainingDuration].Add(sessionPopped);
        //                }
        //                else
        //                {
        //                    sessionsExplored.Add(remainingDuration, new HashSet<int> { sessionPopped });
        //                }
        //            }
        //        }
        //    }

        //    return remainingDuration;
        //}
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TrainingSessionManagement.Business;
using TrainingSessionManagement.Model;

namespace TrainingSessionManagement.Test
{
    [TestClass]
    public class SessionManagerBusinessTest
    {
        private ISessionManagerBusiness _sessionManagerBusiness;
        private readonly Mock<IFileReader> _fileReaderMock;

        public SessionManagerBusinessTest()
        {
            _fileReaderMock = new Mock<IFileReader>();
            _sessionManagerBusiness = new SessionManagerBusiness(_fileReaderMock.Object);
        }

        [TestMethod]
        public void TestSingleSession()
        {
            var input = new List<SessionInputViewModel>
            {
                new SessionInputViewModel
                {
                    SessionDuration = "60min",
                    SessionName = "Session 1"
                }
            };
            _fileReaderMock.Setup(fileReader => fileReader.GetInput(It.IsAny<string>())).Returns(input);
            var tracks = _sessionManagerBusiness.GetTracks();
            Assert.IsTrue(tracks.Count == 1);
            Assert.IsTrue(tracks.First().Sessions.Count == 2);
            var sessions = tracks.First().Sessions;
            Assert.AreEqual("Session 1", sessions[0].SessionName);
            Assert.AreEqual("09:00 AM", sessions[0].StartTimeText);
            Assert.AreEqual("Lunch", sessions[1].SessionName);
            Assert.AreEqual("12:00 PM", sessions[1].StartTimeText);
        }

        [TestMethod]
        public void TestOnlyMorningSessions()
        {
            var input = new List<SessionInputViewModel>
            {
                new SessionInputViewModel
                {
                    SessionDuration = "60min",
                    SessionName = "Session 1"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "60min",
                    SessionName = "Session 2"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "45min",
                    SessionName = "Session 3"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "15min",
                    SessionName = "Session 4"
                }
            };
            _fileReaderMock.Setup(fileReader => fileReader.GetInput(It.IsAny<string>())).Returns(input);
            var tracks = _sessionManagerBusiness.GetTracks();
            Assert.IsTrue(tracks.Count == 1);
            Assert.IsTrue(tracks.First().Sessions.Count == 5);
            var sessions = tracks.First().Sessions;
            Assert.IsTrue(sessions.Any(x => x.SessionName.Equals("Session 1") && x.Duration.Equals("60min")));
            Assert.IsTrue(sessions.Any(x => x.SessionName.Equals("Session 2") && x.Duration.Equals("60min")));
            Assert.IsTrue(sessions.Any(x => x.SessionName.Equals("Session 3") && x.Duration.Equals("45min")));
            Assert.IsTrue(sessions.Any(x => x.SessionName.Equals("Session 4") && x.Duration.Equals("15min")));
            Assert.AreEqual("Lunch", sessions[4].SessionName);
            Assert.AreEqual("12:00 PM", sessions[4].StartTimeText);
        }

        [TestMethod]
        public void TestSingleAfternoonSessions()
        {
            var input = new List<SessionInputViewModel>
            {
                new SessionInputViewModel
                {
                    SessionDuration = "200min",
                    SessionName = "Session 1"
                }
            };
            _fileReaderMock.Setup(fileReader => fileReader.GetInput(It.IsAny<string>())).Returns(input);
            var tracks = _sessionManagerBusiness.GetTracks();
            Assert.IsTrue(tracks.Count == 1);
            Assert.IsTrue(tracks.First().Sessions.Count == 2);
            var sessions = tracks.First().Sessions;
            Assert.IsTrue(sessions.Any(x => x.SessionName.Equals("Session 1") && x.Duration.Equals("200min")));
            Assert.AreEqual("Sharing Session", sessions[1].SessionName);
            Assert.AreEqual("04:20 PM", sessions[1].StartTimeText);
        }

        [TestMethod]
        public void TestNoValidSessions()
        {
            var input = new List<SessionInputViewModel>
            {
                new SessionInputViewModel
                {
                    SessionDuration = "250min",
                    SessionName = "Session 1"
                }
            };
            _fileReaderMock.Setup(fileReader => fileReader.GetInput(It.IsAny<string>())).Returns(input);
            var tracks = _sessionManagerBusiness.GetTracks();
            Assert.IsTrue(tracks.Count == 1);
            Assert.IsTrue(tracks.First().Sessions.Count == 1);
            var sessions = tracks.First().Sessions;
            Assert.AreEqual("Session 1", sessions[0].SessionName);
            Assert.AreEqual("250min", sessions[0].Duration);
        }

        [TestMethod]
        public void TestOnlyIncompleteMorningSessions()
        {
            var input = new List<SessionInputViewModel>
            {
                new SessionInputViewModel
                {
                    SessionDuration = "50min",
                    SessionName = "Session 1"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "30min",
                    SessionName = "Session 2"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "45min",
                    SessionName = "Session 3"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "5min",
                    SessionName = "Session 4"
                }
            };
            _fileReaderMock.Setup(fileReader => fileReader.GetInput(It.IsAny<string>())).Returns(input);
            var tracks = _sessionManagerBusiness.GetTracks();
            Assert.IsTrue(tracks.Count == 1);
            Assert.IsTrue(tracks.First().Sessions.Count == 5);
            var sessions = tracks.First().Sessions;
            Assert.IsTrue(sessions.Any(x => x.SessionName.Equals("Session 1") && x.Duration.Equals("50min")));
            Assert.IsTrue(sessions.Any(x => x.SessionName.Equals("Session 2") && x.Duration.Equals("30min")));
            Assert.IsTrue(sessions.Any(x => x.SessionName.Equals("Session 3") && x.Duration.Equals("45min")));
            Assert.IsTrue(sessions.Any(x => x.SessionName.Equals("Session 4") && x.Duration.Equals("5min")));
            Assert.AreEqual("Lunch", sessions[4].SessionName);
            Assert.AreEqual("12:00 PM", sessions[4].StartTimeText);
        }

        [TestMethod]
        public void TestIncompleteMorningSessionsWithAfternoonSessions()
        {
            var input = new List<SessionInputViewModel>
            {
                new SessionInputViewModel
                {
                    SessionDuration = "70min",
                    SessionName = "Session 1"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "45min",
                    SessionName = "Session 2"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "45min",
                    SessionName = "Session 3"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "55min",
                    SessionName = "Session 4"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "75min",
                    SessionName = "Session 5"
                }
            };
            _fileReaderMock.Setup(fileReader => fileReader.GetInput(It.IsAny<string>())).Returns(input);
            var tracks = _sessionManagerBusiness.GetTracks();
            Assert.IsTrue(tracks.Count == 1);
            Assert.IsTrue(tracks.First().Sessions.Count == 7);
            var sessions = tracks.First().Sessions;
            Assert.IsTrue(sessions.Any(x => x.SessionName.Equals("Session 1") && x.Duration.Equals("70min")));
            Assert.IsTrue(sessions.Any(x => x.SessionName.Equals("Session 2") && x.Duration.Equals("45min")));
            Assert.IsTrue(sessions.Any(x => x.SessionName.Equals("Session 3") && x.Duration.Equals("45min")));
            Assert.IsTrue(sessions.Any(x => x.SessionName.Equals("Session 4") && x.Duration.Equals("55min")));
            Assert.IsTrue(sessions.Any(x => x.SessionName.Equals("Session 5") && x.Duration.Equals("75min")));
            Assert.AreEqual("Lunch", sessions[3].SessionName);
            Assert.AreEqual("12:00 PM", sessions[3].StartTimeText);
            Assert.AreEqual("Sharing Session", sessions[6].SessionName);
            Assert.AreEqual("04:00 PM", sessions[6].StartTimeText);
        }

        [TestMethod]
        public void TestTwoDayTracks()
        {
            var input = new List<SessionInputViewModel>
            {
                new SessionInputViewModel
                {
                    SessionDuration = "70min",
                    SessionName = "Session 1"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "45min",
                    SessionName = "Session 2"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "45min",
                    SessionName = "Session 3"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "55min",
                    SessionName = "Session 4"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "75min",
                    SessionName = "Session 5"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "60min",
                    SessionName = "Session 6"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "30min",
                    SessionName = "Session 7"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "120min",
                    SessionName = "Session 8"
                },
                new SessionInputViewModel
                {
                    SessionDuration = "90min",
                    SessionName = "Session 9"
                }
            };
            _fileReaderMock.Setup(fileReader => fileReader.GetInput(It.IsAny<string>())).Returns(input);
            var tracks = _sessionManagerBusiness.GetTracks();
            Assert.IsTrue(tracks.Count == 2);
            var sessions = tracks.First().Sessions;
            Assert.IsTrue(sessions.Any(x => x.SessionName.Equals("Lunch") && x.StartTimeText.Equals("12:00 PM")));
            Assert.AreEqual("Sharing Session", sessions[sessions.Count - 1].SessionName);
            Assert.IsTrue(sessions[sessions.Count - 1].StartTime <= 17);
        }
    }
}

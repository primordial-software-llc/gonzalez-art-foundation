using IndexBackend.CloudFlareImUnderAttack;
using NUnit.Framework;

namespace SlideshowCreator.Tests.UnitTests
{
    class CloudFlareImUnderAttackModeTests
    {

        private readonly DecodeChallengeQuestion decodeChallengeQuestion = new DecodeChallengeQuestion();

        [Test]
        public void Test_Decoding_CloudFlares_Im_Under_Attack_Mode_Challenge_Question()
        {
            int decoded = decodeChallengeQuestion.Decode(obfuscated, string.Empty);
            /*
            UNOBFUSCATED OUTPUT FROM CHROME'S CONSOLE BEING INTERPRETTED AS JAVASCRIPT
            NaYZsdG={"nu":+((+!![]+[])+(!+[]+!![]+!![]+!![]))};
            NaYZsdG.nu*=+((!+[]+!![]+!![]+[])+(!+[]+!![]+!![]+!![]+!![]+!![]+!![]+!![]));
            NaYZsdG.nu-=+((+!![]+[])+(!+[]+!![]+!![]+!![]+!![]));
            NaYZsdG.nu*=+((!+[]+!![]+!![]+!![]+[])+(!+[]+!![]+!![]+!![]+!![]+!![]));
            NaYZsdG.nu+=+((!+[]+!![]+!![]+[])+(!+[]+!![]));
            NaYZsdG.nu-=+((!+[]+!![]+!![]+!![]+[])+(+!![]));
            NaYZsdG.nu-=+((!+[]+!![]+!![]+[])+(!+[]+!![]+!![]+!![]));
            23739
            */
            Assert.AreEqual(23739, decoded);
        }

        [Test]
        public void Test_Ten()
        {
            var decoded = decodeChallengeQuestion.ConcatenateInts(
                decodeChallengeQuestion.DecodeObfuscatedPiece("((+!![]+[])+(+[]))"));
            Assert.AreEqual("10", decoded);
        }

        [Test]
        public void Test_Four_With_Padding()
        {
            var decoded = decodeChallengeQuestion.ConcatenateInts(
                decodeChallengeQuestion.DecodeObfuscatedPiece("(!+[]+!![]+!![]+!![]+[])"));
            Assert.AreEqual("4", decoded);
        }

        [Test]
        public void Test_Forty_Five()
        {
            Assert.AreEqual("45",
                decodeChallengeQuestion.ConcatenateInts(
                    decodeChallengeQuestion.DecodeObfuscatedPiece("((!+[]+!![]+!![]+!![]+[])+(!+[]+!![]+!![]+!![]+!![]))")));
        }

        [Test]
        public void Test_Thirty_Four()
        {
            Assert.AreEqual("34",
                decodeChallengeQuestion.ConcatenateInts(
                    decodeChallengeQuestion.DecodeObfuscatedPiece("((!+[]+!![]+!![]+[])+(!+[]+!![]+!![]+!![]))")));
        }

        private string obfuscated = @"var s,t,o,p,b,r,e,a,k,i,n,g,f, NaYZsdG={""nu"":+((+!![]+[])+(!+[]+!![]+!![]+!![]))};
        t = document.createElement('div');
        t.innerHTML=""<a href='/'>x</a>"";
        t = t.firstChild.href;r = t.match(/https?:\/\//)[0];
        t = t.substr(r.length); t = t.substr(0,t.length-1);
        a = document.getElementById('jschl-answer');
        f = document.getElementById('challenge-form');
        ;NaYZsdG.nu*=+((!+[]+!![]+!![]+[])+(!+[]+!![]+!![]+!![]+!![]+!![]+!![]+!![]));NaYZsdG.nu-=+((+!![]+[])+(!+[]+!![]+!![]+!![]+!![]));NaYZsdG.nu*=+((!+[]+!![]+!![]+!![]+[])+(!+[]+!![]+!![]+!![]+!![]+!![]));NaYZsdG.nu+=+((!+[]+!![]+!![]+[])+(!+[]+!![]));NaYZsdG.nu-=+((!+[]+!![]+!![]+!![]+[])+(+!![]));NaYZsdG.nu-=+((!+[]+!![]+!![]+[])+(!+[]+!![]+!![]+!![]));a.value = parseInt(NaYZsdG.nu, 10) + t.length; '; 121'";
    }
}

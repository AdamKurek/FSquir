using System.Runtime.CompilerServices;


[assembly: InternalsVisibleTo("FillSquir")]

namespace tests

{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var DirectionVectorLookingRight = new Class1.SKPoint(1, 0);
            var result = Class1.GetFurtherstDirectionVector(new Class1.SKPoint[] {
            new Class1.SKPoint(0,0),
            new Class1.SKPoint(0,100),
            new Class1.SKPoint(100,0),
            },  new Class1.SKPoint(1, 0));
            Assert.AreEqual(result, new Class1.SKPoint(1,0));
            
        }
    }
}
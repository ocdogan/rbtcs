using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using rbt;

namespace ConsoleApplication44
{
    public class IntKey : RbKey
    {
        private int value;
        public IntKey(int value)
        {
            this.value = value;
        }

        public KeyComparison ComparedTo(RbKey key)
        {
            var diff = this.value - ((IntKey)key).value;
            if (diff > 0)
            {
                return KeyComparison.KeyIsGreater;
            }
            if (diff < 0)
            {
                return KeyComparison.KeyIsLess;
            }
            return KeyComparison.KeysAreEqual;
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public override int GetHashCode()
        {
            return value;
        }
    }


    class Program
    {
        static void Main(string[] args)
		{
			Test1 ();
			Test2 ();

			Console.ReadKey();
		}

		static void Test2()
		{
            var tree = new RbTree(null, null);

            var t1 = DateTime.Now;
            for (int i = 0; i < 1000000; i++)
            {
				RbKey key = new IntKey(i);
                tree.Insert(key, 10 + i);
            }
    
			Console.WriteLine(String.Format("Insert time: {0} sec",
				Convert.ToDecimal (DateTime.Now.Subtract(t1).TotalMilliseconds)/1000));

            t1 = DateTime.Now;

            bool found;
            object value;
            for (int i = 0; i < 1000000; i++)
            {
				RbKey key = new IntKey(i);
				tree.Get(key, out value, out found);
            }

			Console.WriteLine(String.Format("Search time: {0} sec",
				Convert.ToDecimal (DateTime.Now.Subtract(t1).TotalMilliseconds)/1000));

			for (int i = 0; i < 1000000; i++)
			{
				RbKey key = new IntKey(i);
				tree.Delete(key);
			}

			Console.WriteLine(String.Format("Delete time: {0} sec",
				Convert.ToDecimal (DateTime.Now.Subtract(t1).TotalMilliseconds)/1000));
		}

		static void Test1()
		{
			var tree = new RbTree(null, null);

			var t1 = DateTime.Now;

			for (int i = 1; i <= 1000000; i++) {
				RbKey key = new IntKey(i);
				tree.Insert(key, 10 + i);
			}

			Console.WriteLine(String.Format("Insert time: {0} sec", 
				Convert.ToDecimal (DateTime.Now.Subtract(t1).TotalMilliseconds)/1000));

			error err;
			var count = 0;
			var iterator = tree.NewRbIterator((itr, key, value) => {
				count++;
			}, out err);

			if (err != null) {
				return;
			}

			count = 0;
			t1 = DateTime.Now;
			iterator.All(out count, out err);
			Console.WriteLine(String.Format("All completed in: {1} sec with count {0}", count, 
				Convert.ToDecimal (DateTime.Now.Subtract(t1).TotalMilliseconds)/1000));

			count = 0;
			t1 = DateTime.Now;
			RbKey loKey = new IntKey(0);
			RbKey hiKey = new IntKey(2000000);
			iterator.Between (loKey, hiKey, out count, out err);
			Console.WriteLine(String.Format("Between completed in: {1} sec with count {0}", count, 
				Convert.ToDecimal (DateTime.Now.Subtract(t1).TotalMilliseconds)/1000));

			count = 0;
			t1 = DateTime.Now;
			RbKey lessThanKey = new IntKey (900001);
			iterator.LessThan (lessThanKey, out count, out err);
			Console.WriteLine(String.Format("LessThan completed in: {1} sec with count {0}", count, 
				Convert.ToDecimal (DateTime.Now.Subtract(t1).TotalMilliseconds)/1000));

			count = 0;
			t1 = DateTime.Now;
			RbKey greaterThanKey = new IntKey (100000);
			iterator.GreaterThan (greaterThanKey, out count, out err);
			Console.WriteLine(String.Format("GreaterThan completed in: {1} sec with count {0}", count, 
				Convert.ToDecimal (DateTime.Now.Subtract(t1).TotalMilliseconds)/1000));

			count = 0;
			t1 = DateTime.Now;
			RbKey lessOrEqualKey = new IntKey (100000);
			iterator.LessOrEqual(lessOrEqualKey, out count, out err);
			Console.WriteLine(String.Format("LessOrEqual completed in: {1} sec with count {0}", count, 
				Convert.ToDecimal (DateTime.Now.Subtract(t1).TotalMilliseconds)/1000));

			count = 0;
			t1 = DateTime.Now;
			RbKey greaterOrEqualKey = new IntKey (0);
			iterator.GreaterOrEqual(greaterOrEqualKey, out count, out err);
			Console.WriteLine(String.Format("GreaterOrEqual completed in: {1} sec with count {0}", count, 
				Convert.ToDecimal (DateTime.Now.Subtract(t1).TotalMilliseconds)/1000));
		}
    }
}

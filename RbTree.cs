using System;
using System.Collections.Generic;
using System.Threading;

namespace rbt
{
	// ErrNo struct is used for the error code of the error
	public enum ErrNo {
		// ErrNoArgumentNil is used if the function parameter is null
		ErrNoArgumentNil = 1,
		// ErrNoArgumentNilWithName is used if the named function parameter is null
		ErrNoArgumentNilWithName = 2,
		// ErrNoIteratorAlreadyRunning is used if the iterator is already running
		ErrNoIteratorAlreadyRunning = 3,
		// ErrNoIteratorClosed is used if the iterator is closed
		ErrNoIteratorClosed = 4,
		// ErrNoIteratorUninitialized is used if the iterator is uninitialized
		ErrNoIteratorUninitialized = 5
	}

	public interface error 
	{
		string Error();
	}

	public class ErrorDef : error
	{
		private static readonly Dictionary<ErrNo, string> errorStr = new Dictionary<ErrNo, string>{
			{ ErrNo.ErrNoArgumentNil, "Argument cannot be null." },
			{ ErrNo.ErrNoArgumentNilWithName, "Argument '%s' cannot be null." },
			{ ErrNo.ErrNoIteratorAlreadyRunning, "Iterator already running." },
			{ ErrNo.ErrNoIteratorClosed, "Iteration context closed." },
			{ ErrNo.ErrNoIteratorUninitialized, "Iteration context uninitialized." },
		};

		// ErrArgumentNil used if the function parameter is null
		public static readonly error ErrArgumentNil = NewError(ErrNo.ErrNoArgumentNil);
		// ErrIteratorAlreadyRunning used if the iterator is already iterating
		public static readonly error ErrIteratorAlreadyRunning = NewError(ErrNo.ErrNoIteratorAlreadyRunning);
		// ErrIteratorClosed used if the iterator is closed
		public static readonly error ErrIteratorClosed = NewError(ErrNo.ErrNoIteratorClosed);
		// ErrIteratorUninitialized used if the iterator is uninitialized
		public static readonly error ErrIteratorUninitialized = NewError(ErrNo.ErrNoIteratorUninitialized);

		private ErrNo err;
		private string message;

		// NewError creates a new error with the given error no
		public static ErrorDef NewError(ErrNo err) {
			return new ErrorDef{
				err = err,
				message = errorStr[err]
			};
		}

		// NewErrorDetailed creates a new error with the given error no and message
		public static ErrorDef  NewErrorDetailed(ErrNo err, string msg) {
			if (String.IsNullOrEmpty(msg)) {
				msg = errorStr[err];
			}
			return new ErrorDef{
				err = err,
				message = msg, 
			};
		}

		// ArgumentNilError creates a new error with ErrNoArgumentNilWithName error no 
		// and named function parameter
		public static ErrorDef ArgumentNilError(string arg) {
			if (String.IsNullOrEmpty(arg)) {
				return new ErrorDef{
					err = ErrNo.ErrNoArgumentNil,
					message = errorStr[ErrNo.ErrNoArgumentNil],
				};
			}
			return new ErrorDef{
				err = ErrNo.ErrNoArgumentNil,
				message = String.Format(errorStr[ErrNo.ErrNoArgumentNilWithName], arg)
			};
		}

		// Error returns the error message
		public string Error() {
			return this.message;
		}

		// ErrorNo returns the error no
		public ErrNo ErrorNo() {
			return this.err;
		}
	}

	// KeyComparison structure used as result of comparing two keys 
	public enum KeyComparison {
		// KeyIsLess is returned as result of key comparison if the first key is less than the second key
		KeyIsLess = -1,
		// KeysAreEqual is returned as result of key comparison if the first key is equal to the second key
		KeysAreEqual = 0,
		// KeyIsGreater is returned as result of key comparison if the first key is greater than the second key
		KeyIsGreater = 1
	}

	// RbKey interface
	public interface RbKey {
		KeyComparison ComparedTo(RbKey key);
	}

	// rbNode structure used for storing key and value pairs
	internal class rbNode {
		internal RbKey key;
		internal object value;
		internal byte color;
		internal rbNode left, right;
	}

	// KeyValueEvent function used on Insert or Delete operations
	public delegate object KeyValueEvent(RbKey key, object currValue);

	// RbTree structure
	public class RbTree {
		internal const byte red = 0;
		internal const byte black = 1;
		internal const int zeroOrEqual = 0;

		private int count;
		internal rbNode root;
		private KeyValueEvent onInsert;
		private KeyValueEvent onDelete;

		// NewRbTree creates a new RbTree and returns its address
		public RbTree(KeyValueEvent onInsert, KeyValueEvent onDelete) {
			this.onInsert = onInsert;
			this.onDelete = onDelete;
		}

		// NewRbIterator creates a new iterator for the given RbTree
		public RbIterator NewRbIterator(RbIterationCallback callback, out error err) {
			err = null;
			if (callback == null) {
				err = ErrorDef.ArgumentNilError("callback");
				return null;
			}

			return new rbIterationContext{
				tree = this,
				callback = callback,
				state = (long)IterationState.iteratorReady,
			};
		}

		// newRbNode creates a new rbNode and returns its address
		private rbNode newRbNode(RbKey key, object value) {
			var result = new rbNode{
				key = key,
				value = value,
				color = red,
			};
			return result;
		}

		// isRed checks if (node exists and its color is red
		private bool isRed(rbNode node) {
			return node != null && node.color == red;
		}

		// isBlack checks if (node exists and its color is black
		private bool isBlack(rbNode node) { 
			return node != null && node.color == black; 
		}

		// min finds the smallest node key including the given node
		private rbNode min(rbNode node) {
			if (node != null) {
				for (;node.left != null;) {
					node = node.left;
				}
			}
			return node;
		}

		// max finds the greatest node key including the given node
		private rbNode max(rbNode node) {
			if (node != null) {
				for (;node.right != null;) {
					node = node.right;
				}
			}
			return node;
		}

		// floor returns the largest key node in the subtree rooted at x less than or equal to the given key
		private rbNode floor(rbNode node, RbKey key) {
			if (node == null) {
				return null;
			}

			var cmp = key.ComparedTo(node.key);
			switch (cmp) {
			case KeyComparison.KeysAreEqual:
				return node;
			case KeyComparison.KeyIsLess:
				return floor(node.left, key);
			default:
				var fn = floor(node.right, key);
				if (fn != null) {
					return fn;
				}
				return node;
			}
		}

		// ceilig returns the smallest key node in the subtree rooted at x greater than or equal to the given key
		private rbNode ceiling(rbNode node, RbKey key) {  
			if (node == null) {
				return null;
			}

			var cmp = key.ComparedTo(node.key);
			switch (cmp) {
			case KeyComparison.KeysAreEqual:
				return node;
			case KeyComparison.KeyIsGreater:
				return ceiling(node.right, key);
			default:
				var cn = ceiling(node.left, key);
				if (cn != null) {
					return cn;
				}
				return node;
			}
		}

		// flipColor switchs the color of the node from red to black or black to red
		private void flipColor(rbNode node) {
			if (node.color == black) {
				node.color = red;
			} else {
				node.color = black;
			}
		}

		// colorFlip switchs the color of the node and its children from red to black or black to red
		private void colorFlip(rbNode node) {
			flipColor(node);
			flipColor(node.left);
			flipColor(node.right);
		}

		// rotateLeft makes a right-leaning link lean to the left
		private rbNode rotateLeft(rbNode node) {
			var child = node.right;
			node.right = child.left;
			child.left = node;
			child.color = node.color;
			node.color = red;

			return child;
		}

		// rotateRight makes a left-leaning link lean to the right
		private rbNode rotateRight(rbNode node) {
			var child = node.left;
			node.left = child.right;
			child.right = node;
			child.color = node.color;
			node.color = red;

			return child;
		}

		// moveRedLeft makes node.left or one of its children red,
		// assuming that node is red and both children are black.
		private rbNode moveRedLeft(rbNode node) {
			colorFlip(node);
			if (isRed(node.right.left)) {
				node.right = rotateRight(node.right);
				node = rotateLeft(node);
				colorFlip(node);
			}
			return node;
		}

		// moveRedRight makes node.right or one of its children red,
		// assuming that node is red and both children are black.
		private rbNode moveRedRight(rbNode node) {
			colorFlip(node);
			if (isRed(node.left.left)) {
				node = rotateRight(node);
				colorFlip(node);
			}
			return node;
		}

		// balance restores red-black tree invariant
		private rbNode balance(rbNode node) {
			if (isRed(node.right)) {
				node = rotateLeft(node);
			}
			if (isRed(node.left) && isRed(node.left.left)) {
				node = rotateRight(node);
			}
			if (isRed(node.left) && isRed(node.right)) {
				colorFlip(node);
			}
			return node;
		}

		// deleteMin removes the smallest key and associated value from the tree
		private rbNode deleteMin(rbNode node) {
			if (node.left == null) {
				return null;
			}    
			if (isBlack(node.left) && !isRed(node.left.left)) {
				node = moveRedLeft(node);
			}
			node.left = deleteMin(node.left);
			return balance(node);
		}

		// Count returns if (count of the nodes stored.
		private int Count() {
			return this.count;
		}

		// IsEmpty returns if the tree has any node.
		private bool IsEmpty() {
			return this.root == null;
		}

		// Min returns the smallest key in the this.
		public void Min(out RbKey key, out object value)
		{
			key = null;
			value = null;
			if (this.root != null) {
				var result = min(this.root);
				key = result.key;
				value = result.value;
			}
		} 

		// Max returns the largest key in the this.
		public void Max(out RbKey key, out object value)
		{
			key = null;
			value = null;
			if (this.root != null) {
				var result = max(this.root);
				key = result.key;
				value = result.value;
			}
		} 

		// Floor returns the largest key in the tree less than or equal to key
		public void Floor(RbKey key, out RbKey outKey, out object value)
		{
			outKey = null;
			value = null;
			if (key != null && this.root != null) {
				var node = floor(this.root, key);
				if (node != null) {
					outKey = node.key;
					value = node.value;
				}
			}
		}    

		// Ceiling returns the smallest key in the tree greater than or equal to key
		public void Ceiling(RbKey key, out RbKey outKey, out object value)
		{
			outKey = null;
			value = null;
			if (key != null && this.root != null) {
				var node = ceiling(this.root, key);
				if (node != null) {
					outKey = node.key;
					value = node.value;
				}
			}
		}

		// Get returns the stored value if (key found and 'true', 
		// otherwise returns 'false' with second return param if (key not found 
		public void Get(RbKey key, out object value, out bool success)
		{
			value = null;
			success = false;
			if (key != null && this.root != null) {
				var node = this.find(key);
				if (node != null) {
					value = node.value;
					success = true;
				}
			}
		}

		// find returns the node if (key found, otherwise returns null 
		internal rbNode find(RbKey key) {
			for ( var node = this.root; node != null;) { 
				var cmp = key.ComparedTo(node.key);
				switch (cmp) {
				case KeyComparison.KeyIsLess:
					node = node.left;
					break;
				case KeyComparison.KeyIsGreater:
					node = node.right;
					break;
				default:
					return node;
				}    
			}
			return null;
		}

		// Insert inserts the given key and value into the tree
		public void Insert(RbKey key, object value) {
			if (key != null) {
				this.root = this.insertNode(this.root, key, value);
				this.root.color = black;
			}
		}

		// insertNode adds the given key and value into the node
		private rbNode insertNode(rbNode node, RbKey key, object value) {
			if (node == null) {
				this.count++;
				return newRbNode(key, value);
			}

			var cmp = key.ComparedTo(node.key);
			switch (cmp) {
			case KeyComparison.KeyIsLess:
				node.left  = this.insertNode(node.left,  key, value);
				break;
			case KeyComparison.KeyIsGreater:
				node.right = this.insertNode(node.right, key, value);
				break;
			default:
				if (this.onInsert == null) {
					node.value = value;
				} else {
					node.value = this.onInsert(key, value);
				}
				break;
			}
			return balance(node);
		}

		// Delete deletes the given key from the tree
		public void Delete(RbKey key)
		{
			this.root = this.deleteNode(this.root, key);
			if (this.root != null) {
				this.root.color = black;
			}
		}

		// deleteNode deletes the given key from the node
		private rbNode deleteNode(rbNode node, RbKey key) {
			if (node == null) {
				return null;
			}

			var cmp = key.ComparedTo(node.key);
			if (cmp == KeyComparison.KeyIsLess) {
				if (isBlack(node.left) && !isRed(node.left.left)) {
					node = moveRedLeft(node);
				}
				node.left = this.deleteNode(node.left, key);
			} else {
				if (cmp == KeyComparison.KeysAreEqual && this.onDelete != null) {
					var value = this.onInsert(key, node.value);      
					if (value != null) {
						node.value = value;
						return node;
					}
				}

				if (isRed(node.left)) {
					node = rotateRight(node);
				}

				if (isBlack(node.right) && !isRed(node.right.left)) {
					node = moveRedRight(node);
				}

				if (key.ComparedTo(node.key) != KeyComparison.KeysAreEqual) {
					node.right = this.deleteNode(node.right, key);
				} else {
					if (node.right == null) {
						return null;
					}

					var rm = min(node.right);
					node.key   = rm.key;
					node.value = rm.value;
					node.right = deleteMin(node.right);

					rm.left = null;
					rm.right = null;

					this.count--;
				}
			}
			return balance(node);
		}
	}

	// RbIterator interface used for iterating on a RbTree
	public interface RbIterator  {
		// All iterates on all items of the RbTree
		void All(out int count, out error err);
		// Between iterates on the items of the RbTree that the key of the item 
		// is less or equal to loKey and greater or equal to hiKey
		void Between(RbKey loKey, RbKey hiKey, out int count, out error err);
		// ClearData clears all the data stored on the iterator
		void ClearData();
		// Close closes the current iteration, so the iteration stops iterating
		void Close();
		// Closed gives the state of the iterator, 'true' if closed
		bool Closed();
		// CurrentCount gives the count of the items that match the iteration case
		int CurrentCount();
		// LessOrEqual iterates on the items of the RbTree that the key of the item 
		// is less or equal to the given key
		void LessOrEqual(RbKey key, out int count, out error err);
		// LessThan iterates on the items of the RbTree that the key of the item 
		// is less than the given key
		void LessThan(RbKey key, out int count, out error err);
		// GetData returns the data stored on the iterator with the dataKey 
		void GetData(string dataKey, out object value, out bool success);
		// GreaterOrEqual iterates on the items of the RbTree that the key of the item 
		// is greater or equal to the given key
		void GreaterOrEqual(RbKey key, out int count, out error err);
		// GreaterThan iterates on the items of the RbTree that the key of the item 
		// is greater than the given key
		void GreaterThan(RbKey key, out int count, out error err);
		// RemoveData deletes the data stored on the iterator with the dataKey 
		void RemoveData(string dataKey);
		// SetData stores the data with the dataKey on the iterator 
		void SetData(string dataKey, object value);
		// Tree returns the RbTree that the iterator is iterating on
		RbTree Tree();
	}

	// RbIterationCallback is the function used to by the RbIterator 
	// with will be called on iteration match
	public delegate void RbIterationCallback(RbIterator iterator, RbKey key, object value);

	internal enum IterationState : long {
		iteratorReady = 1,
		iteratorWalking = 2,
		iteratorClosed = -1,
		iteratorUninitialized = 0
	}

	internal class rbIterationContext : RbIterator
	{
		internal RbTree tree;
		internal long count;
		internal long state;
		internal RbIterationCallback callback;
		internal object mtx = new object();
		internal Dictionary<string, object> data = new Dictionary<string,object>();

		public static void nilIterationCallback(RbIterator iterator, RbKey key, object value)
		{
		}

		public RbTree Tree()
		{
			return this.tree;
		}

		public int CurrentCount()
		{
			return (int)Interlocked.Read(ref count);
		}

		public void incrementCount()
		{
			Interlocked.Increment(ref count);
		}

		public bool inWalk()
		{
			return Interlocked.Read(ref state) == (long)IterationState.iteratorWalking;
		}

		public bool ready()
		{
			return Interlocked.Read(ref state) == (long)IterationState.iteratorReady;
		}

		public bool Closed()
		{
			return Interlocked.Read(ref state) == (long)IterationState.iteratorClosed;
		}

		public void Close()
		{
			lock (this.mtx)
			{
				this.state = (long)IterationState.iteratorClosed;
				this.callback = nilIterationCallback;
				this.tree = null;
			}
		}

		public void ClearData()
		{
			lock (this.mtx)
			{
				this.data = null;
			}
		}

		public void GetData(string dataKey, out object value, out bool success)
		{
			value = null;
			success = false;
			lock (this.mtx)
			{
				success = (this.data != null) && this.data.TryGetValue(dataKey, out value);
			}
		}

		public void SetData(string dataKey, object value)
		{
			lock (this.mtx)
			{
				if (this.data != null)
				{
					this.data[dataKey] = value;
				}
			}
		}

		public void RemoveData(string dataKey)
		{
			lock (this.mtx)
			{
				if (this.data != null && this.data.ContainsKey(dataKey))
				{
					this.data.Remove(dataKey);
				}
			}
		}

		public void checkStateAndGetTree(out RbTree tree, out error err)
		{
			tree = null;
			err = null;
			lock (this.mtx)
			{
				switch ((IterationState)this.state)
				{
				case IterationState.iteratorWalking:
					err = ErrorDef.ErrIteratorAlreadyRunning;
					return;
				case IterationState.iteratorClosed:
					err = ErrorDef.ErrIteratorClosed;
					return;
				case IterationState.iteratorUninitialized:
					err = ErrorDef.ErrIteratorUninitialized;
					return;
				case IterationState.iteratorReady:
					this.count = 0;
					this.state = (long)IterationState.iteratorWalking;
					break;
				}
				if (this.tree == null)
				{
					err = ErrorDef.ErrIteratorClosed;
					return;
				}
			}
			tree = this.tree;
		}

		public void All(out int count, out error err)
		{
			count = 0;
			RbTree tree;
			this.checkStateAndGetTree(out tree, out err);
			if (err != null)
			{
				return;
			}

			try
			{
				this.walkAll(tree.root);
				count = this.CurrentCount();
			}
			finally
			{
				Interlocked.CompareExchange(ref this.state, (long)IterationState.iteratorReady,
					(long)IterationState.iteratorWalking);
			}
		}

		public void walkAll(rbNode node)
		{
			if (node == null || !this.inWalk())
			{
				return;
			}

			if (node.left != null)
			{
				this.walkAll(node.left);
				if (!this.inWalk())
				{
					return;
				}
			}

			this.incrementCount();
			this.callback(this, node.key, node.value);
			if (!this.inWalk())
			{
				return;
			}

			if (node.right != null)
			{
				this.walkAll(node.right);
			}
		}

		public void Between(RbKey loKey, RbKey hiKey, out int count, out error err)
		{
			count = 0;
			err = null;
			if (loKey == null)
			{
				err = ErrorDef.ArgumentNilError("loKey");
				return;
			}
			if (hiKey == null)
			{
				err = ErrorDef.ArgumentNilError("hiKey");
				return;
			}

			RbTree tree;
			this.checkStateAndGetTree(out tree, out err);
			if (err != null)
			{
				return;
			}

			try
			{
				var cmp = loKey.ComparedTo(hiKey);
				switch (cmp)
				{
				case KeyComparison.KeysAreEqual:
					var node = tree.find(loKey);
					if (node != null)
					{
						count = 1;
						this.callback(this, node.key, node.value);
						return;
					}
					return;
				case KeyComparison.KeyIsGreater:
					var tmp = loKey;
					loKey = hiKey;
					hiKey = tmp;
					break;
				}

				this.walkBetween(tree.root, loKey, hiKey);
				count = this.CurrentCount();
			}
			finally
			{
				Interlocked.CompareExchange(ref this.state, (long)IterationState.iteratorReady,
					(long)IterationState.iteratorWalking);
			}
		}

		public void walkBetween(rbNode node, RbKey loKey, RbKey hiKey)
		{
			if (node == null || !this.inWalk())
			{
				return;
			}

			var cmpLo = (int)loKey.ComparedTo(node.key);
			if (cmpLo < RbTree.zeroOrEqual)
			{
				if (node.left != null)
				{
					this.walkBetween(node.left, loKey, hiKey);
					if (!this.inWalk())
					{
						return;
					}
				}
			}

			var cmpHi = (int)hiKey.ComparedTo(node.key);
			if (cmpLo <= RbTree.zeroOrEqual && cmpHi >= RbTree.zeroOrEqual)
			{
				this.incrementCount();
				this.callback(this, node.key, node.value);
				if (!this.inWalk())
				{
					return;
				}
			}

			if (cmpHi > RbTree.zeroOrEqual)
			{
				if (node.right != null)
				{
					this.walkBetween(node.right, loKey, hiKey);
				}
			}
		}

		public void LessOrEqual(RbKey key, out int count, out error err)
		{
			count = 0;
			err = null;
			if (key == null)
			{
				err = ErrorDef.ArgumentNilError("key");
				return;
			}

			RbTree tree;
			this.checkStateAndGetTree(out tree, out err);
			if (err != null)
			{
				return;
			}

			try
			{
				this.walkLessOrEqual(tree.root, key);
				count = this.CurrentCount();
			}
			finally
			{
				Interlocked.CompareExchange(ref this.state, (long)IterationState.iteratorReady,
					(long)IterationState.iteratorWalking);
			}
		}

		public void walkLessOrEqual(rbNode node, RbKey key) {
			if (node == null || !this.inWalk()) {
				return;
			}

			if (node.left != null) {
				this.walkLessOrEqual(node.left, key);
				if (!this.inWalk()) {
					return;
				}
			}

			var cmp = node.key.ComparedTo(key);
			if (cmp == KeyComparison.KeyIsLess || cmp == KeyComparison.KeysAreEqual) {
				this.incrementCount();
				this.callback(this, node.key, node.value);
				if (!this.inWalk()) {
					return;
				}

				if (node.right != null) {
					this.walkLessOrEqual(node.right, key);
				}  
			}
		}

		public void GreaterOrEqual(RbKey key, out int count, out error err)
		{
			count = 0;
			err = null;
			if (key == null)
			{
				err = ErrorDef.ArgumentNilError("key");
				return;
			}

			RbTree tree;
			this.checkStateAndGetTree(out tree, out err);
			if (err != null)
			{
				return;
			}

			try
			{
				this.walkGreaterOrEqual(tree.root, key);
				count = this.CurrentCount();
			}
			finally
			{
				Interlocked.CompareExchange(ref this.state, (long)IterationState.iteratorReady,
					(long)IterationState.iteratorWalking);
			}
		}

		public void walkGreaterOrEqual(rbNode node, RbKey key)
		{
			if (node == null || !this.inWalk())
			{
				return;
			}

			var cmp = node.key.ComparedTo(key);
			if (cmp == KeyComparison.KeyIsGreater || cmp == KeyComparison.KeysAreEqual)
			{
				if (node.left != null)
				{
					this.walkGreaterOrEqual(node.left, key);
					if (!this.inWalk())
					{
						return;
					}
				}

				this.incrementCount();
				this.callback(this, node.key, node.value);
				if (!this.inWalk())
				{
					return;
				}
			}

			if (node.right != null)
			{
				this.walkGreaterOrEqual(node.right, key);
			}
		}

		public void LessThan(RbKey key, out int count, out error err)
		{
			count = 0;
			err = null;
			if (key == null)
			{
				err = ErrorDef.ArgumentNilError("key");
				return;
			}

			RbTree tree;
			this.checkStateAndGetTree(out tree, out err);
			if (err != null)
			{
				return;
			}

			try
			{
				this.walkLessThan(tree.root, key);
				count = this.CurrentCount();
			}
			finally
			{
				Interlocked.CompareExchange(ref this.state, (long)IterationState.iteratorReady,
					(long)IterationState.iteratorWalking);
			}
		}

		public void walkLessThan(rbNode node, RbKey key)
		{
			if (node == null || !this.inWalk())
			{
				return;
			}

			if (node.left != null)
			{
				this.walkLessThan(node.left, key);
				if (!this.inWalk())
				{
					return;
				}
			}

			if (node.key.ComparedTo(key) == KeyComparison.KeyIsLess)
			{
				this.incrementCount();
				this.callback(this, node.key, node.value);
				if (!this.inWalk())
				{
					return;
				}

				if (node.right != null)
				{
					this.walkLessThan(node.right, key);
				}
			}
		}

		public void GreaterThan(RbKey key, out int count, out error err)
		{
			count = 0;
			err = null;
			if (key == null)
			{
				err = ErrorDef.ArgumentNilError("key");
				return;
			}

			RbTree tree;
			this.checkStateAndGetTree(out tree, out err);
			if (err != null)
			{
				return;
			}

			try
			{
				this.walkGreaterThan(tree.root, key);
				count = this.CurrentCount();
			}
			finally
			{
				Interlocked.CompareExchange(ref this.state, (long)IterationState.iteratorReady,
					(long)IterationState.iteratorWalking);
			}
		}

		public void walkGreaterThan(rbNode node, RbKey key)
		{
			if (node == null || !this.inWalk())
			{
				return;
			}

			if (node.key.ComparedTo(key) == KeyComparison.KeyIsGreater)
			{
				if (node.left != null)
				{
					this.walkGreaterThan(node.left, key);
					if (!this.inWalk())
					{
						return;
					}
				}

				this.incrementCount();
				this.callback(this, node.key, node.value);
				if (!this.inWalk())
				{
					return;
				}
			}

			if (node.right != null)
			{
				this.walkGreaterThan(node.right, key);
			}
		}
	}
}

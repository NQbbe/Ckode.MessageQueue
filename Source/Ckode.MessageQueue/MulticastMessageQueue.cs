using System;
using System.Messaging;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ckode.MessageQueue
{
	public class MulticastMessageQueue<T> : IDisposable
	where T : class
	{
		private bool _disposedValue;
		private System.Messaging.MessageQueue _receiveQueue;
		private System.Messaging.MessageQueue _sendQueue;

		private const string _receiveNamePrefix = @".\Private$\";
		private const string _sendNamePrefix = "FormatName:MULTICAST=";
		private static readonly Regex _ipRegex = new Regex(@"(?<First>2[0-4]\d|25[0-5]|[01]?\d\d?)\.(?<Second>2[0-4]\d|25[0-5]|[01]?\d\d?)\.(?<Third>2[0-4]\d|25[0-5]|[01]?\d\d?)\.(?<Fourth>2[0-4]\d|25[0-5]|[01]?\d\d?)", RegexOptions.Compiled);

		public event Action<T> OnMessage;

		/// <summary>
		/// Create instance of the MulticastMessageQueue
		/// </summary>
		/// <param name="ip">IP to multicast to, should be in the interval 224.0.0.0 to 239.255.255.255</param>
		/// <param name="port">Port to multicast to, should be above 1024, e.g. 8001 is often used</param>
		/// <param name="listenerName">Unique name for this listener, is used to create a local messagequeue</param>
		public MulticastMessageQueue(IPAddress ip, int port, string listenerName)
		{
			VerifyIP(ip);
			_receiveQueue = CreateReceiveQueue(ip, port, listenerName);
			_sendQueue = CreateSendQueue(ip, port);
		}

		private void VerifyIP(IPAddress ip)
		{
			var match = _ipRegex.Match(ip.ToString());
			if (!match.Success)
			{
				throw new ArgumentException("Invalid IPv4: " + ip, nameof(ip));
			}

			var first = int.Parse(match.Groups["First"].Value);
			if (first < 224 || first > 239)
			{
				throw new ArgumentException("IP must be in the interval 224.0.0.0 to 239.255.255.255");
			}
		}

		private System.Messaging.MessageQueue CreateReceiveQueue(IPAddress ip, int port, string listenerName)
		{
			System.Messaging.MessageQueue mq;
			var receiveName = _receiveNamePrefix + listenerName;
			if (System.Messaging.MessageQueue.Exists(receiveName))
			{
				mq = new System.Messaging.MessageQueue(receiveName);
			}
			else
			{
				mq = System.Messaging.MessageQueue.Create(receiveName, false);
				mq.MulticastAddress = $"{ip.ToString()}:{port}";
			}
			mq.Formatter = new BinaryMessageFormatter();
			mq.ReceiveCompleted += Mq_ReceiveCompleted;
			mq.BeginReceive();

			return mq;
		}

		private void Mq_ReceiveCompleted(object sender, ReceiveCompletedEventArgs e)
		{
			var mq = sender as System.Messaging.MessageQueue;
			var msg = mq.EndReceive(e.AsyncResult);

			Task.Factory.StartNew(() =>
			{
				OnMessage?.Invoke(msg.Body as T);
			});

			mq.BeginReceive();
		}

		private System.Messaging.MessageQueue CreateSendQueue(IPAddress ip, int port)
		{
			return new System.Messaging.MessageQueue($"{_sendNamePrefix}{ip}:{port}")
			{
				Formatter = new BinaryMessageFormatter()
			};
		}

		public void Send(T msg)
		{
			_sendQueue.Send(msg);
		}

		#region Dispose
		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_receiveQueue.Dispose();
					_sendQueue.Dispose();
					_receiveQueue = null;
					_sendQueue = null;
				}

				_disposedValue = true;
			}
		}


		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}

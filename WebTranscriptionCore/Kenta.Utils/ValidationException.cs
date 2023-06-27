using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Linq.Expressions;
using System.Reflection;

namespace WebTranscriptionCore {

	[Serializable]
	public class ValidationException : Exception {

		private Object target;
		private String propertyName;

		public ValidationException(String message)
			: base(message) {
		}

		public ValidationException(Object target, String propertyName, String message) : base(message) {
			this.target = target;
			this.propertyName = propertyName;
		}

		protected ValidationException(SerializationInfo info, StreamingContext context) : base(info, context) { }

		public Object Target { get { return this.target; } }

		public String PropertyName { get { return this.propertyName; } }

	}
}

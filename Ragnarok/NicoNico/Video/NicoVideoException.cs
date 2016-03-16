﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ragnarok.NicoNico.Video
{
    /// <summary>
    /// 動画関係の例外です。
    /// </summary>
    public class NicoVideoException : NicoException
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NicoVideoException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NicoVideoException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NicoVideoException(string message, string id)
            : base(message, id)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NicoVideoException(string message, string id,
                                  Exception innerException)
            : base(message, id, innerException)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NicoVideoException(NicoStatusCode code)
            : base(code)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NicoVideoException(NicoStatusCode code, Exception innerException)
            : base(code, innerException)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NicoVideoException(NicoStatusCode code, string id)
            : base(code, id)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NicoVideoException(NicoStatusCode code, string id,
                                  Exception innerException)
            : base(code, id, innerException)
        {
        }
    }
}

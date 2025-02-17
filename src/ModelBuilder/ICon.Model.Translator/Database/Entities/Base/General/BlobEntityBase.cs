﻿namespace Mocassin.Model.Translator
{
    /// <summary>
    ///     Abstract base class for objects that use marshalling to a binary state for storage in the database
    /// </summary>
    public abstract class BlobEntityBase : EntityBase
    {
        /// <summary>
        ///     The total number of bytes for the blob containing the data and header bytes
        /// </summary>
        public virtual int BlobByteCount { get; protected set; }

        /// <summary>
        ///     The number of bytes of the blobs that are header information and not actual data
        /// </summary>
        public virtual int HeaderByteCount { get; protected set; }

        /// <summary>
        ///     The binary data of the entity. Property is for EF data storage only
        /// </summary>
        public byte[] BinaryState { get; set; }

        /// <summary>
        ///     Parses the blob entity object into the binary data and header properties
        /// </summary>
        public abstract void ChangeStateToBinary(IMarshalService marshalService);

        /// <summary>
        ///     Parses the binary data and header properties and populates the object
        /// </summary>
        public abstract void ChangeStateToObject(IMarshalService marshalService);
    }
}
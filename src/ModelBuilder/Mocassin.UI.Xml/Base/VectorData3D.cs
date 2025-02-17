﻿using System.Xml.Serialization;
using Mocassin.Mathematics.ValueTypes;

namespace Mocassin.UI.Data.Base
{
    /// <summary>
    ///     Serializable helper object for serialization of <see cref="Mocassin.Mathematics.ValueTypes.IVector3D" /> data
    /// </summary>
    [XmlRoot]
    public class VectorData3D : ProjectDataObject, IDuplicable<VectorData3D>
    {
        private double a;
        private double b;
        private double c;

        /// <summary>
        ///     Get or set the A coordinate
        /// </summary>
        [XmlAttribute]
        public double A
        {
            get => a;
            set => SetProperty(ref a, value);
        }

        /// <summary>
        ///     Get or set the B coordinate
        /// </summary>
        [XmlAttribute]
        public double B
        {
            get => b;
            set => SetProperty(ref b, value);
        }

        /// <summary>
        ///     Get or set the C coordinate
        /// </summary>
        [XmlAttribute]
        public double C
        {
            get => c;
            set => SetProperty(ref c, value);
        }

        /// <inheritdoc />
        public VectorData3D Duplicate() => new VectorData3D {a = a, b = b, c = c};

        /// <inheritdoc />
        object IDuplicable.Duplicate() => Duplicate();

        /// <summary>
        ///     Get the data as a <see cref="Fractional3D" />
        /// </summary>
        /// <returns></returns>
        public Fractional3D AsFractional3D() => new Fractional3D(A, B, C);

        /// <summary>
        ///     Creates a new serializable <see cref="VectorData3D" /> from a passed <see cref="IVector3D" /> interface
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static VectorData3D Create(IVector3D vector) => new VectorData3D {A = vector.Coordinates.A, B = vector.Coordinates.B, C = vector.Coordinates.C};

        /// <inheritdoc />
        public override string ToString() => $"({A.ToString(DefaultCultureInfo)}, {B.ToString(DefaultCultureInfo)}, {C.ToString(DefaultCultureInfo)})";
    }
}
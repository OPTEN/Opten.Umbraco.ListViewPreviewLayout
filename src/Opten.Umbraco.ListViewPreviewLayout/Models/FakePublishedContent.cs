using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Strings;
using Umbraco.Web.Models;

namespace Opten.Umbraco.ListViewPreviewLayout.Models
{
	public static class ContentExtensions
	{
		public static IPublishedContent ToPublishedContent(this IContent content, bool isPreview = false)
		{
			return PublishedContentModelFactoryResolver.Current.Factory.CreateModel(new FakePublishedContent(content, isPreview));
		}
	}

	public class FakePublishedContent : PublishedContentWithKeyBase
	{
		private readonly PublishedContentType contentType;

		private readonly IContent inner;

		private readonly bool isPreviewing;

		private readonly Lazy<string> lazyCreatorName;

		private readonly Lazy<string> lazyUrlName;

		private readonly Lazy<string> lazyWriterName;

		private readonly IPublishedProperty[] properties;

		public FakePublishedContent(IContent inner, bool isPreviewing)
		{
			if (inner == null)
			{
				throw new NullReferenceException("inner");
			}

			this.inner = inner;
			this.isPreviewing = isPreviewing;

			this.lazyUrlName = new Lazy<string>(() => this.inner.GetUrlSegment().ToLower());
			this.lazyCreatorName = new Lazy<string>(() => this.inner.GetCreatorProfile().Name);
			this.lazyWriterName = new Lazy<string>(() => this.inner.GetWriterProfile().Name);

			this.contentType = PublishedContentType.Get(PublishedItemType.Content, this.inner.ContentType.Alias);

			this.properties =
				MapProperties(
					this.contentType.PropertyTypes,
					this.inner.Properties,
					(t, v) => new FakePublishedProperty(t, v, this.isPreviewing)).ToArray();
		}

		public override int Id
		{
			get
			{
				return this.inner.Id;
			}
		}

		public override Guid Key
		{
			get
			{
				return this.inner.Key;
			}
		}

		public override int DocumentTypeId
		{
			get
			{
				return this.inner.ContentTypeId;
			}
		}

		public override string DocumentTypeAlias
		{
			get
			{
				return this.inner.ContentType.Alias;
			}
		}

		public override PublishedItemType ItemType
		{
			get
			{
				return PublishedItemType.Content;
			}
		}

		public override string Name
		{
			get
			{
				return this.inner.Name;
			}
		}

		public override int Level
		{
			get
			{
				return this.inner.Level;
			}
		}

		public override string Path
		{
			get
			{
				return this.inner.Path;
			}
		}

		public override int SortOrder
		{
			get
			{
				return this.inner.SortOrder;
			}
		}

		public override Guid Version
		{
			get
			{
				return this.inner.Version;
			}
		}

		public override int TemplateId
		{
			get
			{
				return this.inner.Template == null ? 0 : this.inner.Template.Id;
			}
		}

		public override string UrlName
		{
			get
			{
				return this.lazyUrlName.Value;
			}
		}

		public override DateTime CreateDate
		{
			get
			{
				return this.inner.CreateDate;
			}
		}

		public override DateTime UpdateDate
		{
			get
			{
				return this.inner.UpdateDate;
			}
		}

		public override int CreatorId
		{
			get
			{
				return this.inner.CreatorId;
			}
		}

		public override string CreatorName
		{
			get
			{
				return this.lazyCreatorName.Value;
			}
		}

		public override int WriterId
		{
			get
			{
				return this.inner.WriterId;
			}
		}

		public override string WriterName
		{
			get
			{
				return this.lazyWriterName.Value;
			}
		}

		public override bool IsDraft
		{
			get
			{
				return this.inner.Published == false;
			}
		}

		public override IPublishedContent Parent
		{
			get
			{
				var parent = this.inner.Parent();
				if (parent != null)
				{
					return parent.ToPublishedContent(this.isPreviewing);
				}
				return null;
			}
		}

		public override IEnumerable<IPublishedContent> Children
		{
			get
			{
				var children = this.inner.Children().ToList();

				return
					children.Select(x => x.ToPublishedContent(this.isPreviewing))
						.Where(x => x != null)
						.OrderBy(x => x.SortOrder);
			}
		}

		public override ICollection<IPublishedProperty> Properties
		{
			get
			{
				return this.properties;
			}
		}

		public override PublishedContentType ContentType
		{
			get
			{
				return this.contentType;
			}
		}

		public override IPublishedProperty GetProperty(string alias)
		{
			return this.properties.FirstOrDefault(x => x.PropertyTypeAlias.InvariantEquals(alias));
		}

		internal static IEnumerable<IPublishedProperty> MapProperties(
			IEnumerable<PublishedPropertyType> propertyTypes,
			IEnumerable<Property> properties,
			Func<PublishedPropertyType, object, IPublishedProperty> map)
		{
			var propertyEditorResolver = PropertyEditorResolver.Current;
			var dataTypeService = ApplicationContext.Current.Services.DataTypeService;

			return propertyTypes.Select(
				x =>
				{
					var p = properties.SingleOrDefault(xx => xx.Alias == x.PropertyTypeAlias);
					var v = p == null || p.Value == null ? null : p.Value;
					if (v != null)
					{
						var e = propertyEditorResolver.GetByAlias(x.PropertyEditorAlias);

						if (e != null)
						{
							v = e.ValueEditor.ConvertDbToString(p, p.PropertyType, dataTypeService);
						}
					}

					return map(x, v);
				});
		}
	}

	internal static class ContentBaseExtensions
	{
		private static IEnumerable<IUrlSegmentProvider> UrlSegmentProviders
		{
			get
			{
				return UrlSegmentProviderResolver.HasCurrent
						   ? UrlSegmentProviderResolver.Current.Providers
						   : new IUrlSegmentProvider[] { new DefaultUrlSegmentProvider() };
			}
		}

		public static string GetUrlSegment(this IContentBase content)
		{
			var url = UrlSegmentProviders.Select(p => p.GetUrlSegment(content)).First(u => u != null);
			url = url ?? new DefaultUrlSegmentProvider().GetUrlSegment(content); // be safe
			return url;
		}

		public static string GetUrlSegment(this IContentBase content, CultureInfo culture)
		{
			var url = UrlSegmentProviders.Select(p => p.GetUrlSegment(content, culture)).First(u => u != null);
			url = url ?? new DefaultUrlSegmentProvider().GetUrlSegment(content, culture); // be safe
			return url;
		}
	}

	[Serializable]
	[XmlType(Namespace = "http://umbraco.org/webservices/")]
	internal class FakePublishedProperty : FakePublishedPropertyBase
	{
		private readonly object dataValue;

		private readonly bool isPreviewing;

		public FakePublishedProperty(PublishedPropertyType propertyType, object dataValue, bool isPreviewing)
			: base(propertyType)
		{
			this.dataValue = dataValue;
			this.isPreviewing = isPreviewing;
		}

		public override bool HasValue
		{
			get
			{
				return this.dataValue != null
					   && ((this.dataValue is string) == false
						   || string.IsNullOrWhiteSpace((string)this.dataValue) == false);
			}
		}

		public override object DataValue
		{
			get
			{
				return this.dataValue;
			}
		}

		public override object Value
		{
			get
			{
				var source = this.PropertyType.ConvertDataToSource(this.dataValue, this.isPreviewing);
				return this.PropertyType.ConvertSourceToObject(source, this.isPreviewing);
			}
		}

		public override object XPathValue
		{
			get
			{
				var source = this.PropertyType.ConvertDataToSource(this.dataValue, this.isPreviewing);
				return this.PropertyType.ConvertSourceToXPath(source, this.isPreviewing);
			}
		}
	}

	internal abstract class FakePublishedPropertyBase : IPublishedProperty
	{
		public readonly PublishedPropertyType PropertyType;

		protected FakePublishedPropertyBase(PublishedPropertyType propertyType)
		{
			if (propertyType == null)
			{
				throw new ArgumentNullException("propertyType");
			}

			this.PropertyType = propertyType;
		}

		public string PropertyTypeAlias
		{
			get
			{
				return this.PropertyType.PropertyTypeAlias;
			}
		}

		public abstract bool HasValue { get; }

		public abstract object DataValue { get; }

		public abstract object Value { get; }

		public abstract object XPathValue { get; }
	}

}

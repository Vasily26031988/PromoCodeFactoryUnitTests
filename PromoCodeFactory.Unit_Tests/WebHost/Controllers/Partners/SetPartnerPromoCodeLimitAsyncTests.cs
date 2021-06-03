using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Castle.Components.DictionaryAdapter;
using FluentAssertions;
using FluentAssertions.Common;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PromoCodeFactory.Core.Abstractions.Repositories;
using PromoCodeFactory.Core.Domain.PromoCodeManagement;
using PromoCodeFactory.WebHost.Controllers;
using PromoCodeFactory.WebHost.Models;
using Xunit;

namespace PromoCodeFactory.Unit_Tests.WebHost.Controllers.Partners
{
	public class SetPartnerPromoCodeLimitAsyncTests
	{
		private readonly IFixture _fixture;
		private readonly Mock<IRepository<Partner>> _partnerRepositoryMock;
		private readonly PartnersController _partnersController;


		public SetPartnerPromoCodeLimitAsyncTests()
		{
			_fixture = new Fixture().Customize(new AutoMoqCustomization());
			_partnerRepositoryMock = _fixture.Freeze<Mock<IRepository<Partner>>>();
			_partnersController = _fixture
				.Build<PartnersController>()
				.OmitAutoProperties()
				.Create();
		}

		/// <summary>
		/// Если партнер не найден, то необходимо выдать ошибку 404
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task SetPartnerPromoCodeLimitAsync_PartnerNotFound_ReturnsNotFound()
		{
			//Arrange
			var partnerId = _fixture.Create<Guid>();
			var partner = default(Partner);
			_partnerRepositoryMock
				.Setup(x => x.GetByIdAsync(partnerId))
				.ReturnsAsync(partner);
			var request = _fixture.Create<SetPartnerPromoCodeLimitRequest>();


			//Act
			var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, request);
			
			//Assert
			result.Should().BeAssignableTo<NotFoundResult>();
		}

		/// <summary>
		/// Если партнер не активен и поле IsActive=false в классе Partner, необходимо выдать ошибку 400
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task SetPartnerPromoCodeLimitAsync_PartnerNotActive_ShouldReturnBadRequest()
		{
			//Arrange
			var partnerId = _fixture.Create<Guid>();
			var partner = _fixture
				.Build<Partner>()
				.With(x => x.IsActive, false)
				.Without(x => x.PartnerLimits)
				.Create();
			_partnerRepositoryMock
				.Setup(x => x.GetByIdAsync(partnerId))
				.ReturnsAsync(partner);
			var request = _fixture.Create<SetPartnerPromoCodeLimitRequest>();
			

			//Act
			var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, request);

			//Assert
			result.Should().BeAssignableTo<BadRequestObjectResult>();
		}

		/// <summary>
		/// если лимит закончился, то количество не обнуляется
		/// </summary>
		/// <returns></returns>
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task SetPartnerPromocodeLimitAsync_NumberIssuedPromocodes_ShouldBeZeroIfActiveLimit(bool activeLimit)
		{
			//Arrange
			var partnerId = _fixture.Create<Guid>();
			var partner = _fixture
				.Build<Partner>()
				.Without(x => x.PartnerLimits)
				.Create();
			var limit = _fixture
				.Build<PartnerPromoCodeLimit>()
				.With(x=>x.Partner, partner)
				.With(x => x.CancelDate,
				() => activeLimit ? (DateTime?)null : DateTime.Now)
				.Create();

			partner.PartnerLimits = new List<PartnerPromoCodeLimit>() {limit};
			_partnerRepositoryMock
				.Setup(x => x.GetByIdAsync(partnerId))
				.ReturnsAsync(partner);
			var request = _fixture.Create<SetPartnerPromoCodeLimitRequest>();
			
			//Act
			var result = _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, request);


			//Assert
			partner.NumberIssuedPromoCodes.Should().Match(val => activeLimit ? val == 0 : val > 0);

		}

		/// <summary>
		/// При установке лимита нужно отключить предыдущий лимит
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task SetPartnerPromoCodeLimitAsync_PreviousLimit_ShouldBeClosed()
		{
			//Arrange
			var partnerId = _fixture.Create<Guid>();
			var partner = _fixture
				.Build<Partner>()
				.Without(x => x.PartnerLimits)
				.Create();
			var limit = _fixture
				.Build<PartnerPromoCodeLimit>()
				.With(x => x.PartnerId, () => partnerId)
				.With(x => x.Partner, () => partner)
				.Without(x => x.CancelDate)
				.Create();
			partner.PartnerLimits = new List<PartnerPromoCodeLimit>() {limit};
			_partnerRepositoryMock
				.Setup(x => x.GetByIdAsync(partnerId))
				.ReturnsAsync(partner);
			var request = _fixture.Create<SetPartnerPromoCodeLimitRequest>();



			//Act
			var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, request);


			//Assert
			limit.CancelDate.Should().NotBeNull();

		}

		/// <summary>
		////Лимит должен быть больше нуля
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task SetPartnerPromoCodeLimitAsync_Limit_ShouldBeGreaterThenZero()
		{
			//Arrange 
			var partnerId = _fixture.Create<Guid>();
			var partner = _fixture
				.Build<Partner>()
				.Without(x => x.PartnerLimits)
				.Create();
			var limit = _fixture
				.Build<PartnerPromoCodeLimit>()
				.With(x => x.PartnerId, () => partnerId)
				.With(x => x.Partner, () => partner)
				.With(x => x.Limit)
				.Create();
			partner.PartnerLimits = new List<PartnerPromoCodeLimit> {limit};
			_partnerRepositoryMock
				.Setup(x => x.GetByIdAsync(partnerId))
				.ReturnsAsync(partner);
			var request = _fixture.Create<SetPartnerPromoCodeLimitRequest>();




			//Act 
			var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, request);

			
			
			//Assert
			limit.Limit.Should().BePositive();


		}

	}
}

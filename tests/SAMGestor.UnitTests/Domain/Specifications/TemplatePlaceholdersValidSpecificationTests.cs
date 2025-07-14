using System;
using Xunit;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Specifications;

namespace SAMGestor.UnitTests.Domain.Specifications;

public class TemplatePlaceholdersValidSpecificationTests
{
    [Fact]
    public void Should_Return_True_When_All_Placeholders_Are_Allowed()
    {
        var tpl = new MessageTemplate(
            TemplateType.Selection,
            "Olá {{nome}}, você foi contemplado para o retiro {{retiro}}!",
            hasPlaceholders: true);

        var spec = new TemplatePlaceholdersValidSpecification();

        Assert.True(spec.IsSatisfiedBy(tpl));
    }

    [Fact]
    public void Should_Return_False_When_Unknown_Placeholder_Is_Present()
    {
        var tpl = new MessageTemplate(
            TemplateType.GeneralNotice,
            "Mensagem genérica para {{nome}} – código {{fooBar}}",
            hasPlaceholders: true);

        var spec = new TemplatePlaceholdersValidSpecification();

        Assert.False(spec.IsSatisfiedBy(tpl));
    }
}
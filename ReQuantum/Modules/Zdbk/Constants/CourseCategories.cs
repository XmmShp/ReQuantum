using System;
using System.Collections.Generic;
using System.Linq;
using ReQuantum.Modules.Zdbk.Enums;

namespace ReQuantum.Modules.Zdbk.Constants;

/// <summary>
/// 课程分类记录
/// </summary>
/// <param name="Id">分类ID</param>
/// <param name="Name">分类名称</param>
public record CourseCategoryRecord(CourseCategory Id, string Name);

/// <summary>
/// 课程分类常量和映射
/// </summary>
public static class CourseCategories
{
    /// <summary>
    /// 所有课程分类
    /// </summary>
    public static readonly List<CourseCategoryRecord> All =
    [
        new CourseCategoryRecord(CourseCategory.MyCategory, "本专业课程"),
        new CourseCategoryRecord(CourseCategory.CompulsoryAll, "全部必修课程"),
        new CourseCategoryRecord(CourseCategory.CompulsoryIpm, "思政类/军体类"),
        new CourseCategoryRecord(CourseCategory.CompulsoryLan, "外语类"),
        new CourseCategoryRecord(CourseCategory.CompulsoryCom, "计算机类"),
        new CourseCategoryRecord(CourseCategory.CompulsoryEtp, "创新创业类"),
        new CourseCategoryRecord(CourseCategory.CompulsorySci, "自然科学通识类"),
        new CourseCategoryRecord(CourseCategory.ElectiveAll, "全部选修课程"),
        new CourseCategoryRecord(CourseCategory.ElectiveChC, "中华传统"),
        new CourseCategoryRecord(CourseCategory.ElectiveGlC, "世界文明"),
        new CourseCategoryRecord(CourseCategory.ElectiveSoc, "当代社会"),
        new CourseCategoryRecord(CourseCategory.ElectiveSci, "科技创新"),
        new CourseCategoryRecord(CourseCategory.ElectiveArt, "文艺审美"),
        new CourseCategoryRecord(CourseCategory.ElectiveBio, "生命探索"),
        new CourseCategoryRecord(CourseCategory.ElectiveTec, "博雅技艺"),
        new CourseCategoryRecord(CourseCategory.ElectiveGec, "通识核心课程"),
        new CourseCategoryRecord(CourseCategory.PhysicalEdu, "体育课程"),
        new CourseCategoryRecord(CourseCategory.MajorFundation, "专业基础课程"),
        new CourseCategoryRecord(CourseCategory.MyMajor, "本专业"),
        new CourseCategoryRecord(CourseCategory.AllMajor, "所有专业"),
        new CourseCategoryRecord(CourseCategory.AccreditedAll, "全部认定课程"),
        new CourseCategoryRecord(CourseCategory.AccreditedArt, "美育类"),
        new CourseCategoryRecord(CourseCategory.AccreditedLbr, "劳育类"),
        new CourseCategoryRecord(CourseCategory.International, "国际化课程"),
        new CourseCategoryRecord(CourseCategory.Ckc, "竺可桢学院课程"),
        new CourseCategoryRecord(CourseCategory.Honor, "荣誉课程")
    ];

    /// <summary>
    /// 必修课程分类
    /// </summary>
    public static readonly List<CourseCategoryRecord> CompulsoryCourses = All.Where(c =>
        c.Id is CourseCategory.CompulsoryAll or
        CourseCategory.CompulsoryIpm or
        CourseCategory.CompulsoryLan or
        CourseCategory.CompulsoryCom or
        CourseCategory.CompulsoryEtp or
        CourseCategory.CompulsorySci).ToList();

    /// <summary>
    /// 选修课程分类
    /// </summary>
    public static readonly List<CourseCategoryRecord> ElectiveCourses = All.Where(c =>
        c.Id is CourseCategory.ElectiveAll or
        CourseCategory.ElectiveChC or
        CourseCategory.ElectiveGlC or
        CourseCategory.ElectiveSoc or
        CourseCategory.ElectiveSci or
        CourseCategory.ElectiveArt or
        CourseCategory.ElectiveBio or
        CourseCategory.ElectiveTec or
        CourseCategory.ElectiveGec).ToList();

    /// <summary>
    /// 专业课程分类
    /// </summary>
    public static readonly List<CourseCategoryRecord> MajorCourses = All.Where(c =>
        c.Id is CourseCategory.MyMajor or
        CourseCategory.AllMajor or
        CourseCategory.MajorFundation).ToList();

    /// <summary>
    /// 认定课程分类
    /// </summary>
    public static readonly List<CourseCategoryRecord> AccreditedCourses = All.Where(c =>
        c.Id is CourseCategory.AccreditedAll or
        CourseCategory.AccreditedArt or
        CourseCategory.AccreditedLbr).ToList();

    /// <summary>
    /// 特殊课程分类
    /// </summary>
    public static readonly List<CourseCategoryRecord> SpecialCourses = All.Where(c =>
        c.Id is CourseCategory.International or
        CourseCategory.Ckc or
        CourseCategory.Honor).ToList();

    /// <summary>
    /// 获取课程类别对应的API参数
    /// </summary>
    /// <param name="category">课程分类</param>
    /// <returns>元组：(dl: 大类, lx: 类型, xkmc: 选课名称)</returns>
    public static (string dl, string lx, string? xkmc) GetCourseType(CourseCategory category) => category switch
    {
        CourseCategory.MyCategory => ("xk_1", "bl", "本类(专业)选课"),
        CourseCategory.CompulsoryAll => ("xk_b", "bl", "全部课程"),
        CourseCategory.CompulsoryIpm => ("E", "zl", "思政类\\军体类"),
        CourseCategory.CompulsoryLan => ("F", "zl", "外语类"),
        CourseCategory.CompulsoryCom => ("G", "zl", "计算机类"),
        CourseCategory.CompulsoryEtp => ("P", "zl", "创新创业类"),
        CourseCategory.CompulsorySci => ("T", "zl", "自然科学通识类"),
        CourseCategory.ElectiveAll => ("xk_n", "bl", "全部课程"),
        CourseCategory.ElectiveChC => ("zhct", "zl", "中华传统"),
        CourseCategory.ElectiveGlC => ("sjwm", "zl", "世界文明"),
        CourseCategory.ElectiveSoc => ("ddsh", "zl", "当代社会"),
        CourseCategory.ElectiveSci => ("kjcx", "zl", "科技创新"),
        CourseCategory.ElectiveArt => ("wysm", "zl", "文艺审美"),
        CourseCategory.ElectiveBio => ("smts", "zl", "生命探索"),
        CourseCategory.ElectiveTec => ("byjy", "zl", "博雅技艺"),
        CourseCategory.ElectiveGec => ("xhxk", "zl", "通识核心课程"),
        CourseCategory.PhysicalEdu => ("xk_8", "bl", "体育课程"),
        CourseCategory.MajorFundation => ("xk_zyjckc", "bl", "专业基础课程"),
        CourseCategory.MyMajor => ("zy_b", "bl", "本类(专业)"),
        CourseCategory.AllMajor => ("zy_qb", "bl", "所有类(专业)"),
        CourseCategory.AccreditedAll => ("xk_rdxkc", "bl", "qbkc"),
        CourseCategory.AccreditedArt => ("xk_rdxkc", "zl", "美育类"),
        CourseCategory.AccreditedLbr => ("xk_rdxkc", "zl", "劳育类"),
        CourseCategory.International => ("gjhkc", "zl", "国际化课程"),
        CourseCategory.Ckc => ("Z", "bl", "竺可桢学院课程"),
        CourseCategory.Honor => ("R", "bl", "荣誉课程"),
        CourseCategory.Undefined => throw new ArgumentException("未定义的课程分类", nameof(category)),
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "未知的课程分类")
    };
}
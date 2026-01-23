# Zdbk 模块重构扩展实施计划

> **项目目标**：将 `~/RiderProjects/Quantum/zdbkservice` 的完整 ZDBK 选课功能整合到 ReQuantum 项目中

## 📋 核心目标

- ✅ **功能互补**：整合"已选课程表"和"可选课程信息"
- ✅ **复用基础设施**：SSO 认证、状态管理、加密存储
- ✅ **避免重复**：不重复实现登录、Token 刷新
- ✅ **统一状态**：共享 ZdbkState，减少资源占用

---

## 🎯 实施阶段（按优先级排序）

### 阶段 1：基础设施准备 ⭐⭐⭐

#### 任务 1.1：迁移枚举类型
- **创建** `ReQuantum/Modules/Zdbk/Enums/CourseCategory.cs`
  - 25 个课程分类（MyCategory, CompulsoryAll, CompulsoryIpm...）
- **验证** `ReQuantum/Modules/Zdbk/Enums/CourseStatus.cs`
  - 确保包含：Unknown, NotSelected, Selected, Passed, Failed

#### 任务 1.2：迁移常量定义
- **创建** `ReQuantum/Modules/Zdbk/Constants/CourseCategories.cs`
  - `CourseCategoryRecord(CourseCategory Id, string Name)` record
  - `All` 列表（25 个分类元数据）
  - 分组列表：CompulsoryCourses, ElectiveCourses, MajorCourses...
  - 课程类别到 API 参数的映射方法（GetCourseType）

- **创建** `ReQuantum/Modules/Zdbk/Constants/PlotFlag.cs`
  - 位标志常量（SectionMask, WeekTypeMask, DayOfWeekMask, TermMask）
  - 位偏移量（WeekTypeOffset, DayOfWeekOffset, TermOffset）
  - 编码/解码方法

#### 任务 1.3：扩展 ZdbkState
- **修改** `ReQuantum/Modules/Zdbk/Models/ZdbkState.cs`
  - 新增字段：`Grade`, `AcademicYear`, `Semester`
  - 保持向后兼容（使用可选参数）

**源文件路径**：
- `~/RiderProjects/Quantum/zdbkservice/Enums/*`
- `~/RiderProjects/Quantum/zdbkservice/Constants/*`

---

### 阶段 2：核心模型层迁移 ⭐⭐⭐

#### 任务 2.1：迁移课程模型
- **创建** `ReQuantum/Modules/Zdbk/Models/Course.cs`
  - 基类：Id, Name, Credits, Category, WeekTime, Department, Property, Introduction, Sections
- **创建** `ReQuantum/Modules/Zdbk/Models/StatefulCourse.cs`
  - 继承 Course，添加 Status 字段
- **重写** `ReQuantum/Modules/Zdbk/Models/SelectableCourse.cs`
  - 继承 StatefulCourse，添加 Code 字段（选课课号）

#### 任务 2.2：迁移教学班模型
- **验证** `ReQuantum/Modules/Zdbk/Models/Section.cs`
  - 确保包含：Id, Course, Instructors, ScheduleAndLocations, ExamTime, IsInternationalCourse, TeachingForm, LessonForm
- **创建** `ReQuantum/Modules/Zdbk/Models/SelectableSection.cs`
  - 继承 Section，添加：AvailableSeats, TotalSeats, MajorWaitingCount, TotalWaitingCount
  - 计算属性：SelectionProbability
- **创建** `ReQuantum/Modules/Zdbk/Models/SectionSnapshot.cs`
  - Record 类，用于序列化和缓存
  - 扁平化存储课程和教学班信息

#### 任务 2.3：迁移时间模型
- **创建** `ReQuantum/Modules/Zdbk/Models/TimeSlot.cs`
  - `record TimeSlot(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime)`
  - 包含时间解析方法（从 "2025年06月21日(14:00-16:00)" 解析）

**源文件路径**：
- `~/RiderProjects/Quantum/zdbkservice/Models/*`

---

### 阶段 3：工具类和解析器 ⭐⭐

#### 任务 3.1：迁移冲突检测工具
- **创建** `ReQuantum/Modules/Zdbk/Utilities/SectionSnapshotUtils.cs`
  - 核心方法：`IsConflictWith(SectionSnapshot lhs, SectionSnapshot rhs)`
  - 实现逻辑：
    1. 检查考试时间冲突
    2. 解析课表字符串（"周一第1,2节{单周}"）
    3. 提取学期、周几、单双周、节数
    4. 使用 PlotFlag 位运算比较冲突

#### 任务 3.2：迁移 JSON 序列化器
- **创建** `ReQuantum/Modules/Zdbk/Utilities/SectionSnapshotJsonConverter.cs`
  - 自定义 `JsonConverter<SectionSnapshot>`
  - 处理复杂嵌套结构（TimeSlot、HashSet、ExtraProperties）

**源文件路径**：
- `~/RiderProjects/Quantum/zdbkservice/Utilities/*`

---

### 阶段 4：服务层实现 ⭐⭐⭐

#### 任务 4.1：实现 ZdbkCourseService
**文件**：
- `ReQuantum/Modules/Zdbk/Services/IZdbkCourseService.cs`（接口）
- `ReQuantum/Modules/Zdbk/Services/ZdbkCourseService.cs`（实现）

**接口方法**：
```csharp
Task<Result<HashSet<SelectableCourse>>> GetAvailableCoursesAsync(
    CourseCategory category, int startPage, int endPage);
Task<Result> UpdateSectionsAsync(SelectableCourse course);
Task<Result> UpdateIntroductionAsync(Course course);
CachedEntity<HashSet<SectionSnapshot>> SelectedSections { get; }
Task<Result<HashSet<SectionSnapshot>>> RefreshSelectedSectionsAsync();
```

**实现要点**：
1. **GetAvailableCoursesAsync**：
   - POST `/jwglxt/xsxk/zzxkghb_cxZzxkGhbKcList.html`
   - 使用 CourseCategories 映射参数（dl, lx, xkmc）
   - 解析 JSON 响应构造 SelectableCourse

2. **UpdateSectionsAsync**：
   - POST `/jwglxt/xsxk/zzxkghb_cxZzxkGhbJxbList.html`
   - 解析容量和等待人数，计算选中概率

3. **UpdateIntroductionAsync**：
   - GET `/jwglxt/xkjjsc/kcjjck_cxXkjjPage.html`
   - 正则提取课程介绍

4. **RefreshSelectedSectionsAsync**：
   - POST `/jwglxt/xsxk/zzxkghb_cxZzxkGhbChoosed.html`
   - 获取已选课程并缓存

#### 任务 4.2：实现 ZdbkGraduationService
**文件**：
- `ReQuantum/Modules/Zdbk/Services/IZdbkGraduationService.cs`（接口）
- `ReQuantum/Modules/Zdbk/Services/ZdbkGraduationService.cs`（实现）

**接口方法**：
```csharp
CachedEntity<HashSet<SelectableCourse>> GraduationRequirements { get; }
Task<Result<HashSet<SelectableCourse>>> RefreshGraduationRequirementsAsync();
```

**实现要点**：
1. GET `/jwglxt/bysh/byshck_cxByshzsIndex.html`
2. 使用 HtmlAgilityPack 解析课程表格
3. 状态映射（"已通过" → Passed, "未通过" → Failed...）
4. 缓存机制（24 小时过期）

#### 任务 4.3：扩展 ZdbkSessionService
**文件**：`ReQuantum/Modules/Zdbk/Services/ZdbkSessionService.cs`（修改）

**修改内容**：
1. 在 `GetAuthenticatedClientAsync` 中：
   - 访问 `/jwglxt/xsxk/zzxkghb_cxZzxkGhbIndex.html`
   - 使用 HtmlAgilityPack 解析学生详细信息
   - 提取字段：StudentId, StudentName, Grade, Major, AcademicYear, Semester
   - 更新 ZdbkState 包含所有新字段

**源文件路径**：
- `~/RiderProjects/Quantum/zdbkservice/ZdbkService.cs`（参考实现）

---

### 阶段 5：序列化和集成 ⭐

#### 任务 5.1：更新 SourceGenerationContext
**文件**：`ReQuantum/Infrastructure/SourceGenerationContext.cs`（修改）

**新增类型标注**：
```csharp
[JsonSerializable(typeof(Course))]
[JsonSerializable(typeof(StatefulCourse))]
[JsonSerializable(typeof(SelectableCourse))]
[JsonSerializable(typeof(Section))]
[JsonSerializable(typeof(SelectableSection))]
[JsonSerializable(typeof(SectionSnapshot))]
[JsonSerializable(typeof(TimeSlot))]
[JsonSerializable(typeof(HashSet<SelectableCourse>))]
[JsonSerializable(typeof(HashSet<SectionSnapshot>))]
[JsonSerializable(typeof(List<SelectableSection>))]
```

#### 任务 5.2：验证 AutoInject 注册
**确认**：
- ZdbkCourseService 标记 `[AutoInject(Lifetime.Singleton)]`
- ZdbkGraduationService 标记 `[AutoInject(Lifetime.Singleton, IDaemonService)]`
- 构建项目，验证源生成器正确注册

---

### 阶段 6：测试和验证 ⭐

#### 任务 6.1：集成测试
**验证流程**：
1. 登录 → 获取 ZdbkState → 验证学生信息完整
2. 遍历课程类别 → 获取课程列表 → 验证数据格式
3. 选择课程 → 获取教学班 → 验证容量和概率
4. 获取已选课程 → 验证与课程表一致性
5. 获取毕业要求 → 验证状态映射

#### 任务 6.2：向后兼容性验证
**确认**：
- 现有课程表功能不受影响
- ZdbkState 扩展保持旧代码兼容
- 现有服务正常运行

---

## 📁 最终文件结构

```
ReQuantum/Modules/Zdbk/
├── Constants/
│   ├── ClassTimeTable.cs          (现有)
│   ├── CourseCategories.cs        (新增) ✨
│   └── PlotFlag.cs                (新增) ✨
├── Enums/
│   ├── CourseCategory.cs          (新增) ✨
│   └── CourseStatus.cs            (现有，验证)
├── Models/
│   ├── AcademicCalendar.cs        (现有)
│   ├── Course.cs                  (新增) ✨
│   ├── ParsedCourseInfo.cs        (现有)
│   ├── Section.cs                 (现有，验证)
│   ├── SelectableCourse.cs        (重写) ✨
│   ├── SelectableSection.cs       (新增) ✨
│   ├── SectionSnapshot.cs         (新增) ✨
│   ├── StatefulCourse.cs          (新增) ✨
│   ├── TimeSlot.cs                (新增) ✨
│   ├── ZdbkSectionDto.cs          (现有)
│   ├── ZdbkSectionScheduleResponse.cs (现有)
│   └── ZdbkState.cs               (扩展) ✨
├── Parsers/
│   └── CourseInfoParser.cs        (现有)
├── Services/
│   ├── AcademicCalendarService.cs     (现有)
│   ├── ZdbkCalendarConvertService.cs  (现有)
│   ├── ZdbkCourseService.cs           (重写实现) ✨
│   ├── ZdbkGraduationService.cs       (重写实现) ✨
│   ├── ZdbkSectionScheduleService.cs  (现有)
│   └── ZdbkSessionService.cs          (扩展) ✨
└── Utilities/
    ├── SectionSnapshotJsonConverter.cs  (新增) ✨
    └── SectionSnapshotUtils.cs          (新增) ✨
```

**图例**：✨ 表示需要新增或重写的文件

---

## 🔗 API 端点汇总

| 功能 | 方法 | 端点 | 参数 |
|------|------|------|------|
| 获取学生信息 | GET | `/jwglxt/xsxk/zzxkghb_cxZzxkGhbIndex.html` | gnmkdm=N253530 |
| 查询可选课程 | POST | `/jwglxt/xsxk/zzxkghb_cxZzxkGhbKcList.html` | dl, lx, xkmc, nj, xn, xq, zydm, kspage, jspage |
| 查询教学班 | POST | `/jwglxt/xsxk/zzxkghb_cxZzxkGhbJxbList.html` | xn, xq, xkkh |
| 查询已选课程 | POST | `/jwglxt/xsxk/zzxkghb_cxZzxkGhbChoosed.html` | xn, xq |
| 毕业审核 | GET | `/jwglxt/bysh/byshck_cxByshzsIndex.html` | gnmkdm=N305508, su={学号} |
| 课程简介 | GET | `/jwglxt/xkjjsc/kcjjck_cxXkjjPage.html` | xkjjid={课程ID}, gnmkdm=N253530 |

---

## 🔑 关键实现细节

### 课程类别到 API 参数映射（GetCourseType 方法）
需要在 `CourseCategories.cs` 中实现映射方法：

```csharp
public static (string dl, string lx, string? xkmc) GetCourseType(CourseCategory category)
{
    return category switch
    {
        CourseCategory.MyCategory => ("xk_1", "bl", "本类(专业)选课"),
        CourseCategory.CompulsoryAll => ("B", "zl", null),
        CourseCategory.CompulsoryIpm => ("E", "zl", null),
        CourseCategory.CompulsoryLan => ("B", "bl", "外语类"),
        CourseCategory.CompulsoryCom => ("B", "bl", "计算机类"),
        CourseCategory.CompulsoryEtp => ("B", "bl", "创新创业类"),
        CourseCategory.CompulsorySci => ("B", "bl", "自然科学通识类"),
        CourseCategory.ElectiveAll => ("X", "zl", null),
        CourseCategory.ElectiveChC => ("zhct", "zl", null),
        CourseCategory.ElectiveGlC => ("sjwm", "zl", null),
        CourseCategory.ElectiveSoc => ("ddsh", "zl", null),
        CourseCategory.ElectiveSci => ("kjcx", "zl", null),
        CourseCategory.ElectiveArt => ("wysm", "zl", null),
        CourseCategory.ElectiveBio => ("smts", "zl", null),
        CourseCategory.ElectiveTec => ("byjy", "zl", null),
        CourseCategory.ElectiveGec => ("X", "bl", "通识核心课程"),
        CourseCategory.PhysicalEdu => ("xk_ty", "bl", "体育课程"),
        CourseCategory.MajorFundation => ("xk_jc", "bl", "专业基础课程"),
        CourseCategory.MyMajor => ("xk_b", "bl", "本专业"),
        CourseCategory.AllMajor => ("xk_b", "zl", null),
        CourseCategory.AccreditedAll => ("xk_rd", "zl", null),
        CourseCategory.AccreditedArt => ("xk_rd", "bl", "美育类"),
        CourseCategory.AccreditedLbr => ("xk_rd", "bl", "劳育类"),
        CourseCategory.International => ("gjh", "zl", null),
        CourseCategory.Ckc => ("ckc", "zl", null),
        CourseCategory.Honor => ("ry", "zl", null),
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };
}
```

### 容量和等待人数解析
```csharp
// 容量：rs = "2/30" → available=2, total=30
var parts = rs.Split('/');
var available = int.Parse(parts[0]);
var total = int.Parse(parts[1]);

// 等待人数：yxrs = "5~10" → major=5, total=10
var waitParts = yxrs.Split('~');
var majorWaiting = int.Parse(waitParts[0]);
var totalWaiting = int.Parse(waitParts[1]);
```

### 选中概率计算
```csharp
public decimal SelectionProbability =>
    AvailableSeats <= 0 ? 0.00m :
        TotalWaitingCount > 0 && TotalWaitingCount > AvailableSeats ?
            decimal.Round((decimal)AvailableSeats / TotalWaitingCount, 2) :
            1.00m;
```

---

## ⚠️ 注意事项

### 向后兼容性
- ZdbkState 使用可选参数，不破坏现有代码
- ZdbkSessionService 保持现有方法签名
- 新增服务使用独立接口

### 数据隐私
- Cookie 和学生信息使用加密存储
- 不在日志中记录敏感信息

### 错误处理
- 所有网络请求使用 try-catch
- 返回 `Result<T>` 类型
- JSON 解析失败时返回友好错误消息

### 性能优化
- 使用 CachedEntity 缓存
- 支持分页加载
- 避免重复请求

---

## ✅ 完成标准

1. ✓ 所有新增文件已创建并编译通过
2. ✓ ZdbkCourseService 和 ZdbkGraduationService 实现完整功能
3. ✓ ZdbkState 扩展完成且保持向后兼容
4. ✓ SourceGenerationContext 更新并生成代码成功
5. ✓ 集成测试通过，验证所有 API 端点可用
6. ✓ 现有课程表功能不受影响
7. ✓ 代码符合 ReQuantum 项目规范

---

## 🚀 预期成果

重构完成后，ReQuantum 的 Zdbk 模块将提供：

1. **完整的可选课程查询**：支持 25 种课程分类，分页加载
2. **教学班详细信息**：容量、等待人数、选中概率计算
3. **已选课程管理**：查询、缓存、冲突检测
4. **毕业审核信息**：课程要求、完成状态追踪
5. **课程简介获取**：详细课程描述
6. **统一状态管理**：扩展的 ZdbkState 包含完整学生信息
7. **高效缓存机制**：减少网络请求，提升性能
8. **类型安全**：编译时 JSON 序列化

**用户价值**：
- 一站式查看所有可选课程
- 智能冲突检测，避免选课冲突
- 毕业进度追踪，确保满足毕业要求
- 流畅的用户体验

---

## 📝 参考资料

**源项目路径**：`~/RiderProjects/Quantum/zdbkservice`

**关键源文件**：
- `ZdbkService.cs` - 主要业务逻辑参考
- `Models/*.cs` - 模型定义
- `Constants/*.cs` - 常量和映射
- `Utilities/*.cs` - 工具类

**目标项目路径**：`/Users/master/RiderProjects/ReQuantum`

**现有 Zdbk 模块**：`ReQuantum/Modules/Zdbk/`
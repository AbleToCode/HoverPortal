---
description: HoverPortal 项目开发工作流 (Standard Workflow)
---

## 开发工作流 (Development Workflow)

###1 流程总览
本工作流采用 **“思索-调研-存证-执行”** 的闭环模式。通过 MCP 工具链，将 AI 从一个简单的代码生成器提升为具备系统架构分析能力的“高级合伙人”。

###2 阶段详解 (Stages Description)

#### 第一阶段：深度逻辑推演 (Logic Deduction)
*   **工具**: `sequential-thinking`
*   **描述**: 在编写任何底层代码前，Agent 必须启动思维链。分析 Windows 系统消息（如 `WM_MOUSEHOVER`）与桌面进程（`explorer.exe`）的交互边界，推演出潜在的冲突点（例如：全屏应用检测、多显示器坐标偏移）。
*   **产出**: 逻辑流程图与异常处理方案。

#### 第二阶段：全球方案调研 (Global Research)
*   **工具**: `github MCP`
*   **描述**: 利用 GitHub 搜索功能检索近两年内、高星级的 Windows 桌面增强开源项目。重点寻找有关 `LVM_GETITEMRECT`（图标坐标）和 `DirectComposition`（系统级动效）的稳定实现。
*   **产出**: 技术调研报告、参考代码片段、第三方库选型建议。

#### 第三阶段：架构记忆存证 (Memory Anchoring)
*   **工具**: `context7`
*   **描述**: 将第一、二阶段确定的关键技术参数（如：窗口类名、API 结构体定义、Z-Order 层级逻辑）存储至 `context7`。这保证了即使在开启新对话后，Agent 依然能瞬间找回项目的“灵魂”架构。
*   **产出**: 项目上下文快照。

#### 第四阶段：防御性编码 (Safe Coding)
*   **工具**: `C# / .NET 8 / Win32 API`
*   **描述**: 根据存证的架构编写代码。要求必须包含内存管理逻辑（如 `SafeHandle`）和异步加载逻辑（防止阻塞桌面 UI）。所有 P/Invoke 签名需经过 GitHub 验证。
*   **产出**: 可编译的模块化源码。

#### 第五阶段：动态测试与优化 (Test & Refine)
*   **工具**: `Playwright` (辅助测试) / `sequential-thinking`
*   **描述**: 通过思维链分析运行日志，排查内存泄漏。若出现卡顿，重新进入第一阶段优化渲染管线 (Rendering Pipeline)。
*   **产出**: 性能优化报告、稳定版二进制文件。

###3 工作流执行规范 (Rules of Execution)

| 动作 | 强制要求 |
| :--- | :--- |
| **遇到复杂 Bug** | 必须先调用 `sequential-thinking` 进行步骤拆解，严禁盲目尝试代码。 |
| **引入新 API** | 必须先通过 `github` 验证该 API 在不同 Windows 版本（10/11）的兼容性。 |
| **模块交接** | 每个阶段结束，必须使用 `context7` 更新项目的“记忆槽位”，确保进度不丢失。 |
| **动画实现** | 严禁使用 UI 线程动画，必须采用 `Composition API` 以保证 60FPS 丝滑感。 |
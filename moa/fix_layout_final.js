const fs = require('fs');
const path = 'g:/محمد حسن/mo/moa/moa/Views/Shared/_Layout.cshtml';

try {
    let content = fs.readFileSync(path, 'utf8');
    
    // Using a very liberal regex to find the mobile nav block by its unique classes and structure
    const navRegex = /<nav[^>]*navbar-glass[^>]*md:hidden[^>]*>[\s\S]*?<\/nav>/;

    if (navRegex.test(content)) {
        console.log('Found navigation block using regex.');
        
        const newBlock = `    @if (User?.Identity?.IsAuthenticated == true)
    {
        <nav class="fixed bottom-4 left-4 right-4 z-[99] md:hidden overflow-hidden rounded-3xl border border-indigo-100 bg-gradient-to-r from-indigo-600 via-indigo-500 to-violet-500 shadow-xl" aria-label="التنقل السفلي">
            <div class="flex items-center justify-around px-2 py-2">

                @* الرئيسية *@
                <a asp-controller="Home" asp-action="Index"
                   class="flex flex-col items-center gap-1 rounded-2xl p-2 transition-colors @(ctrl == "home" ? "bg-white/20 text-white" : "text-indigo-100 hover:bg-white/10 hover:text-white")">
                    <svg class="h-6 w-6" viewBox="0 0 24 24" fill="none"><path d="M2.25 12l8.95-8.95a1.5 1.5 0 0 1 2.1 0L22.25 12M4.5 9.75v10.5a.75.75 0 0 0 .75.75h4.5a.75.75 0 0 0 .75-.75v-6h3v6a.75.75 0 0 0 .75.75h4.5a.75.75 0 0 0 .75-.75V9.75" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>
                    <span class="text-[10px] font-semibold">الرئيسية</span>
                </a>

                @* سحب جديد *@
                <a asp-controller="Receipts" asp-action="Create"
                   class="flex flex-col items-center gap-1 rounded-2xl p-2 transition-colors @(ctrl == "receipts" ? "bg-white/20 text-white" : "text-indigo-100 hover:bg-white/10 hover:text-white")">
                    <svg class="h-6 w-6" viewBox="0 0 24 24" fill="none"><path d="M7 7h10M7 11h10M7 15h6M6 3h12a2 2 0 0 1 2 2v16l-3-2-3 2-3-2-3 2-3-2-3 2V5a2 2 0 0 1 2-2Z" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>
                    <span class="text-[10px] font-semibold">سحب</span>
                </a>

                @* بحث *@
                <a asp-controller="Cards" asp-action="Search"
                   class="flex flex-col items-center gap-1 rounded-2xl p-2 transition-colors @(ctrl == "cards" && act == "search" ? "bg-white/20 text-white" : "text-indigo-100 hover:bg-white/10 hover:text-white")">
                    <svg class="h-6 w-6" viewBox="0 0 24 24" fill="none"><path d="M10 10a4 4 0 1 0 0.001-8.001A4 4 0 0 0 10 10Z" stroke="currentColor" stroke-width="1.5"/><path d="M21 21l-5.2-5.2" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/></svg>
                    <span class="text-[10px] font-semibold">بحث</span>
                </a>

                @* مجموعات *@
                <a asp-controller="CardGroups" asp-action="Index"
                   class="flex flex-col items-center gap-1 rounded-2xl p-2 transition-colors @(ctrl == "cardgroups" ? "bg-white/20 text-white" : "text-indigo-100 hover:bg-white/10 hover:text-white")">
                    <svg class="h-6 w-6" viewBox="0 0 24 24" fill="none"><path d="M7 7h14M7 12h14M7 17h14M3 7h1M3 12h1M3 17h1" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/></svg>
                    <span class="text-[10px] font-semibold">المجموعات</span>
                </a>

                @* بيع حر *@
                <a asp-controller="FreeSales" asp-action="Create"
                   class="flex flex-col items-center gap-1 rounded-2xl p-2 transition-colors @(ctrl == "freesales" ? "bg-white/20 text-white" : "text-indigo-100 hover:bg-white/10 hover:text-white")">
                    <svg class="h-6 w-6" viewBox="0 0 24 24" fill="none"><path d="M12 6v12M6 12h12" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/><circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.5"/></svg>
                    <span class="text-[10px] font-semibold">بيع حر</span>
                </a>

                @* القائمة (المزيد) *@
                <button type="button" @click="mobileMenuOpen = true"
                        class="flex flex-col items-center gap-1 rounded-2xl p-2 transition-colors text-indigo-100 hover:bg-white/10 hover:text-white">
                    <svg class="h-6 w-6" viewBox="0 0 24 24" fill="none"><path d="M4 6h16M4 12h16M4 18h16" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>
                    <span class="text-[10px] font-semibold">المزيد</span>
                </button>
            </div>
        </nav>
    }`;

        content = content.replace(navRegex, newBlock);
        fs.writeFileSync(path, content, 'utf8');
        console.log('Successfully updated _Layout.cshtml');
    } else {
        console.log('Could not find the navigation block in _Layout.cshtml using regex.');
    }
} catch (e) {
    console.error('Error:', e.message);
}

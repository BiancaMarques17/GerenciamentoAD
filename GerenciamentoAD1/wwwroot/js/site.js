// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


document.addEventListener("DOMContentLoaded", function () {
    fetchGroupsFromAPI();
    fetchOUsFromAPI();
});

// Variáveis globais
let availableGroups = []; // Todos os grupos disponíveis
let selectedGroups = []; // Grupos adicionados pelo usuário

// Atualiza o nome completo automaticamente
function atualizarNome() {
    let nome = document.getElementById("id_nome").value.trim();
    let sobrenome = document.getElementById("id_sobrenome").value.trim();
    document.getElementById("id_nome_visivel").value = `${nome} ${sobrenome}`;
}

// Aplicar máscara no telefone
function mascaraTelefone(input) {
    let num = input.value.replace(/\D/g, "");
    input.value = num.length > 10
        ? num.replace(/^(\d{2})(\d{5})(\d{4})/, "($1) $2-$3")
        : num.replace(/^(\d{2})(\d{4})(\d{0,4})/, "($1) $2-$3");
}

// Aplicar máscara no CPF
function mascaraCPF(input) {
    let num = input.value.replace(/\D/g, "");
    input.value = num
        .replace(/^(\d{3})(\d)/, "$1.$2")
        .replace(/^(\d{3})\.(\d{3})(\d)/, "$1.$2.$3")
        .replace(/\.(\d{3})(\d)/, ".$1-$2");
}

// Função para carregar a lista de OUs da API
function fetchOUsFromAPI() {
    fetch("http://localhost:5148/api/ad/organizational-units")
        .then(response => response.json())
        .then(data => populateOUDropdown(data))
        .catch(error => console.error("Erro ao buscar OUs:", error));
}

// Popula o campo de seleção de OUs
function populateOUDropdown(ous) {
    const ouSelect = document.getElementById("ouSelect");
    ouSelect.innerHTML = "<option value=''>Selecione uma OU</option>";
    ous.forEach(ou => {
        let option = document.createElement("option");
        option.value = ou;
        option.textContent = ou;
        ouSelect.appendChild(option);
    });
}

// Função para carregar a lista de grupos da API
function fetchGroupsFromAPI() {
    fetch("http://localhost:5148/api/ad/list-groups")
        .then(response => response.json())
        .then(data => {
            availableGroups = data.map(group => ({
                nome: group.nome,
                caminho: group.caminho
            }));
            updateGroupOffcanvas();
        })
        .catch(error => console.error("Erro ao carregar grupos:", error));
}


// Atualiza o offcanvas com os grupos disponíveis
function updateGroupOffcanvas() {
    let tableBody = document.getElementById("groupList");
    tableBody.innerHTML = ""; // Limpa a lista do offcanvas

    availableGroups.forEach(group => {
        if (!selectedGroups.some(g => g.nome === group.nome)) { // Apenas os que ainda não foram selecionados
            let row = document.createElement("tr");
            row.innerHTML = `
                <td>${group.nome}</td>
                <td>${group.caminho}</td>
                <td style="text-align: center;">
                    <button class="btn btn-primary btn-sm" onclick="addGroup('${group.nome}', '${group.caminho}')">Adicionar</button>
                </td>
            `;
            tableBody.appendChild(row);
        }
    });
}

// Função para filtrar os grupos com base no texto inserido na busca
function filterGroups() {
    let searchValue = document.getElementById("search").value.toLowerCase(); // Obtém o valor da pesquisa e converte para minúsculo
    let tableBody = document.getElementById("groupList");
    tableBody.innerHTML = ""; // Limpa a lista antes de atualizá-la

    // Filtra os grupos com base no valor da pesquisa
    availableGroups.forEach(group => {
        // Verifica se o nome ou caminho do grupo contém o texto da pesquisa
        if (group.nome.toLowerCase().includes(searchValue) || group.caminho.toLowerCase().includes(searchValue)) {
            if (!selectedGroups.some(g => g.nome === group.nome)) { // Apenas os grupos que não foram selecionados
                let row = document.createElement("tr");
                row.innerHTML = `
                    <td>${group.nome}</td>
                    <td>${group.caminho}</td>
                    <td style="text-align: center;">
                        <button class="btn btn-primary btn-sm" onclick="addGroup('${group.nome}', '${group.caminho}')">Adicionar</button>
                    </td>
                `;
                tableBody.appendChild(row);
            }
        }
    });
}


// Adiciona um grupo à lista selecionada
function addGroup(nome, caminho) {
    let group = { nome, caminho };

    if (!selectedGroups.some(g => g.nome === nome)) { // Verifica se já foi adicionado
        selectedGroups.push(group);
        updateSelectedTable();
        updateGroupOffcanvas(); // Atualiza o offcanvas removendo o grupo adicionado
    }
}



// Atualiza a tabela de grupos selecionados
function updateSelectedTable() {
    let selectedTable = document.getElementById("selectedGroups");
    selectedTable.innerHTML = "";

    selectedGroups.forEach((group, index) => {
        let row = document.createElement("tr");
        row.innerHTML = `
            <td>${group.nome}</td>
            <td>${group.caminho}</td>
            <td style="text-align: center;">
                <button class="btn btn-danger btn-sm" onclick="removeGroup(${index})">Remover</button>
            </td>
        `;
        selectedTable.appendChild(row);
    });
}

// Remove um grupo da lista e retorna ao offcanvas
function removeGroup(index) {
    let removedGroup = selectedGroups.splice(index, 1)[0]; // Remove e retorna o grupo removido
    updateSelectedTable();
    updateGroupOffcanvas(); // Atualiza o offcanvas para trazer o grupo de volta
}

// Remove máscaras antes de enviar para a API
function removerMascara(valor) {
    return valor.replace(/\D/g, "");
}

// Função para converter a OU para o formato do Active Directory
function formatarOUparaAD(mascara) {
    return mascara
        .split(' > ')          // Divide a string pelos separadores " > "
        .reverse()             // Inverte a ordem dos elementos
        .map(ou => `OU=${ou}`) // Adiciona o prefixo "OU=" a cada elemento
        .join(',');            // Junta tudo separado por vírgulas
}

function timestampToFILETIMEBrasilia(timestampMs) {
    try {
        // 1. Criar um objeto Date a partir do timestamp
        const utcDate = new Date(timestampMs);

        // 2. Ajustar para o fuso horário de Brasília (UTC-3)
        const brasiliaOffsetMinutes = -180; // -3 horas * 60 minutos
        const brasiliaDate = new Date(utcDate.getTime() + brasiliaOffsetMinutes * 60 * 1000);

        // 3. Calcular o FileTime
        const FILETIME_EPOCH = new Date("1601-01-01T00:00:00Z").getTime();
        const msSinceEpoch = brasiliaDate.getTime();
        const fileTime = (msSinceEpoch - FILETIME_EPOCH) * 10000;

        return fileTime.toString(); // Retorna como string
    } catch (error) {
        console.error("Erro ao converter timestamp para FileTime:", error.message);
        return null; // Ou outro valor padrão, como "0", dependendo da sua necessidade
    }
}

function formatarDataHora(data) {
    const ano = data.getFullYear();
    const mes = String(data.getMonth() + 1).padStart(2, '0'); // Mês começa em 0
    const dia = String(data.getDate()).padStart(2, '0');
    const hora = String(data.getHours()).padStart(2, '0');
    const minutos = String(data.getMinutes()).padStart(2, '0');
    const segundos = String(data.getSeconds()).padStart(2, '0');
    return `${ano}-${mes}-${dia} ${hora}:${minutos}:${segundos}`;
}

function salvarUsuario() {
    let nome = document.getElementById("id_nome").value.trim();
    let sobrenome = document.getElementById("id_sobrenome").value.trim();
    let nomeVisivel = document.getElementById("id_nome_visivel").value.trim();
    let cargo = document.getElementById("id_cargo").value.trim();
    let setor = document.getElementById("id_setor").value.trim();
    let telefone = document.getElementById("id_telefone").value.trim();
    let cpf = document.getElementById("id_cpf").value.trim();
    let login = document.getElementById("id_login").value.trim();
    let ouSelecionada = document.getElementById("ouSelect").value;

    // Opções de conta
    //let alterarSenha = document.getElementById("id_check1").checked;
    let senhaNuncaExpira = document.getElementById("id_check2").checked;
    let contaDesabilitada = document.getElementById("id_check3").checked;

    // Vencimento de conta
    let dataExpiracao = document.getElementById("id_radio2").checked ? document.getElementById("id_data").value : 0;

    if (!nome || !sobrenome || !login || !cpf || !ouSelecionada) {
        alert("Todos os campos obrigatórios devem ser preenchidos!");
        return;
    }

    // Converter a OU para o formato correto (ajustando para incluir o domínio)
    let ouFormatada = formatarOUparaAD(ouSelecionada);

    // Converter a data de expiração para filetime se fornecida
    if (dataExpiracao) {
        let data = new Date(dataExpiracao);
        dataExpiracao = timestampToFILETIMEBrasilia(data); // Converte para filetime
    }

    let userData = {
        Login: login,
        Name: nome,
        Surname: sobrenome,
        Departamento: setor,
        Cargo: cargo,
        //AlterarSenha: alterarSenha,
        SenhaNuncaExpira: senhaNuncaExpira,
        ContaDesabilitada: contaDesabilitada,
        DataExpiracao: dataExpiracao, // Agora é o valor em filetime
        Telefone: removerMascara(telefone),
        NomeCompleto: nomeVisivel,
        CPF: removerMascara(cpf),
        OrganizationalUnit: ouFormatada, // Agora no formato correto
        Grupos: selectedGroups.map(g => g.caminho.toString()) // Envia o Distinguished Name (DN)
    };

    console.log(userData);

    fetch("http://localhost:5148/api/ad/create-user", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(userData)
    })
        .then(response => {
            if (!response.ok) {
                throw new Error("Erro na resposta da API");
            }
            return response.json();
        })
        .then(data => alert("Usuário criado com sucesso!"))
        .catch(error => alert("Erro ao criar usuário: " + error.message));
}
